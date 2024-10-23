/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;

namespace TowerDefenseMP
{
    /// <summary>
    /// Custom implementation of the Unity Networking NetworkManager class. This script is responsible
    /// for connecting to Unity Relay and Lobby, adding player connections and handling disconnects.
    /// </summary>
	public class NetworkManagerCustom : NetworkManager, INetworkPrefabInstanceHandler
    {
        /// <summary>
        /// Fired when establishing a connection to Unity Relay or Lobby fails or the player disconnects in other ways.
        /// </summary>
        public static event Action<string> connectionFailedEvent;

        /// <summary>
        /// Fired when receiving a list of lobby matches from either Unity Lobby or a LAN broadcast.
        /// </summary>
        public static event Action<List<Lobby>> lobbyListUpdateEvent;

        /// <summary>
        /// Fired after the server updates its lobby data, e.g. by making it public.
        /// </summary>
        public static event Action<Lobby> lobbyDataUpdateEvent;

        /// <summary>
        /// Maximum amount of players per room. This should also be set in the Unity Lobby dashboard.
        /// </summary>
        public int maxPlayers = 8;

        /// <summary>
        /// References to the available player prefabs.
        /// </summary>
        public GameObject[] playerPrefabs;

        /// <summary>
        /// Name of the lobby scene that gets loaded when a user decides to starts a server.
        /// Clients do not need to load this since the active scene is synced to them automatically.
        /// </summary>
        public string lobbyScene;

        /// <summary>
        /// Reference to the lobby that was either created or joined by this user.
        /// </summary>
        public Lobby currentLobby;

        /// <summary>
        /// Access to the network mode to be used in this class, initialized by UIMain.
        /// </summary>
        [HideInInspector]
        public NetworkMode networkMode;

        /// <summary>
        /// Array storing PlayerStruct data coming from the Lobby scene, so that we can continue
        /// using that data in the map scenes later as well. This is not a NetworkList because
        /// the NetworkManager cannot have a NetworkObject component attached to it for syncing
        /// </summary>
		[HideInInspector]
        public PlayerStruct[] playerData;

        /// <summary>
        /// Reference to the LAN broadcasting and receiver component.
        /// </summary>
        [HideInInspector]
        public NetworkDiscovery discovery;

        //caching reference to coroutine keeping the lobby alive
        private Coroutine lobbyHeartbeatCoroutine;

        //static reference to this script
        private static NetworkManagerCustom instance;


        /// <summary>
        /// Returns a static reference to this script.
        /// </summary>
        public static NetworkManagerCustom GetInstance()
        {
            return instance;
        }


        //initialize variables
        void Awake()
        {
            //make sure we keep one instance of this script. Usually we would keep the same instance,
            //but due to a NetCode bug not shutting down correctly, we destroy the other existing instance
            //instead so we can start fresh like there has not been any NetworkManager instance before
            if (instance != null && instance != this)
            {
                Destroy(instance.gameObject);
                SetSingleton();
            }

            //set static references
            discovery = GetComponent<NetworkDiscovery>();
            instance = this;
        }


        //initialize callbacks
        void Start()
        {
            OnServerStarted += OnStartServer;
            ConnectionApprovalCallback += ApprovalCheck;
            OnClientDisconnectCallback += OnClientDisconnect;
        }


        //since the allowed max connections count was previously stored in UnetTransport which is no longer used,
        //we have to rely on Connection Approval requests to deny players on full or offline games. This should
        //mostly happen in LAN games, as full online games are quickly removed from the lobby list
        void ApprovalCheck(ConnectionApprovalRequest request, ConnectionApprovalResponse response)
        {
            if (request.ClientNetworkId != LocalClientId && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != lobbyScene) { response.Reason = "Match has already started."; }
            if (!IsServer && networkMode == NetworkMode.Offline) { response.Reason = "Private Match."; }
            if (ConnectedClientsIds.Count >= maxPlayers) { response.Reason = "Server Full."; }

            //request was declined
            if (!string.IsNullOrEmpty(response.Reason))
            {
                response.Approved = false;
                return;
            }

            response.Approved = true;
        }


        /// <summary>
        /// Starts initializing and connecting to a game. Depends on the selected network mode.
        /// </summary>
        public static void StartMatch(string connectData)
        {
            switch (instance.networkMode)
            {
                //tries to retrieve a list of all games currently available on Unity Lobby
                case NetworkMode.Online:
                    instance.StartOnline(connectData);
                    break;

                //search for open LAN games on the current network
                case NetworkMode.LAN:
                    instance.StartLAN(connectData);
                    break;

                //start a single LAN game but do not make it public over the network (offline)
                case NetworkMode.Offline:
                    instance.StartOffline();
                    break;
            }
        }


        /// <summary>
        /// Initiates a request for querying Unity Lobby (online) or starts listening to broadcasts (LAN) for open matches.
        /// </summary>
        public static async void QueryLobbies()
        {
            switch (instance.networkMode)
            {
                case NetworkMode.Online:
                    if (UnityServices.State != ServicesInitializationState.Initialized || !AuthenticationService.Instance.IsSignedIn)
                        break;

                    try
                    {
                        QueryLobbiesOptions options = new QueryLobbiesOptions()
                        {
                            Filters = new List<QueryFilter>()
                            {
                                new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                            },
                            Count = 25
                        };

                        QueryResponse lobbies = await LobbyService.Instance.QueryLobbiesAsync(options);
                        lobbyListUpdateEvent?.Invoke(lobbies.Results);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                        lobbyListUpdateEvent?.Invoke(new List<Lobby>());
                    }
                    break;

                case NetworkMode.LAN:
                    //start listening to other hosts
                    List<Lobby> lobbyList = await instance.discovery.StartListenThread();
                    lobbyListUpdateEvent?.Invoke(lobbyList);
                    break;
            }
        }


        /// <summary>
        /// Sends a request with current data to Unity Lobby (online) or re-assigns broadcast data (LAN).
        /// </summary>
        public static async void UpdateLobbyData(UpdateLobbyOptions options)
        {
            if (instance.currentLobby == null) return;

            switch(GetInstance().networkMode)
            {
                case NetworkMode.Online:
                    instance.currentLobby = await LobbyService.Instance.UpdateLobbyAsync(instance.currentLobby.Id, options);
                    break;

                case NetworkMode.LAN:
                    GetInstance().discovery.broadcastData.MapName = options.Data["MapName"].Value;
                    GetInstance().discovery.broadcastData.CurrentPlayers = GetInstance().ConnectedClients.Count;

                    if (options.IsPrivate == true) GetInstance().discovery.Stop();
                    break;
            }

            lobbyDataUpdateEvent?.Invoke(instance.currentLobby);
        }


        //if the lobbyId value passed in is empty, we will start a new online match as a host
        //otherwise, we will try to join the existing lobbyId as a client
        //this makes use of both the RelayService and LobbyService
        async void StartOnline(string lobbyId)
        {
            bool isHost = string.IsNullOrEmpty(lobbyId);
            Singleton.NetworkConfig.ConnectionData = GetJoinMessage();

            if (isHost)
            {
                try
                {
                    //create relay allocation and receive RelayCode
                    Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
                    string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                    //set target connection data in transport component 
                    string connectionType = "udp";
                    #if UNITY_WEBGL
                    connectionType = "wss";
                    #endif

                    RelayServerData serverData = new RelayServerData(allocation, connectionType);
                    (Singleton.NetworkConfig.NetworkTransport as UnityTransport).SetRelayServerData(serverData);

                    //create lobby with game mode and put RelayCode in lobby data
                    //we use an arbitrary name for the lobby since it will not be displayed anyway
                    CreateLobbyOptions lobbyOptions = new CreateLobbyOptions()
                    {
                        IsPrivate = true,
                        Data = new Dictionary<string, DataObject>
                        {
                            { "MapName", new DataObject(DataObject.VisibilityOptions.Public, "UNKNOWN", DataObject.IndexOptions.S1) },
                            { "RelayCode", new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                        }
                    };

                    currentLobby = await LobbyService.Instance.CreateLobbyAsync(AuthenticationService.Instance.Profile, maxPlayers, lobbyOptions);
                    lobbyHeartbeatCoroutine = StartCoroutine(HeartbeatLobbyCoroutine(currentLobby.Id, 25));
                }
                catch (RelayServiceException e)
                {
                    if (e.Reason != RelayExceptionReason.NoError)
                    {
                        Debug.LogError(e);
                        connectionFailedEvent(e.Message);
                        return;
                    }
                }

                Singleton.StartHost();
            }
            else
            { 
                try
                {
                    //try to join lobby and receive relay allocation using RelayCode in lobby data
                    currentLobby = lobbyId.Length > 6 ? await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId) : await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyId);
                    JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(currentLobby.Data["RelayCode"].Value);

                    //connect relay data to lobby player data so disconnects remove the player from the lobby too
                    UpdatePlayerOptions playerOptions = new UpdatePlayerOptions
                    {
                        AllocationId = allocation.AllocationId.ToString()
                    };
                    await LobbyService.Instance.UpdatePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId, playerOptions);

                    //set target connection data in transport component
                    string connectionType = "udp";
                    #if UNITY_WEBGL
                    connectionType = "wss";
                    #endif

                    RelayServerData serverData = new RelayServerData(allocation, connectionType);
                    (Singleton.NetworkConfig.NetworkTransport as UnityTransport).SetRelayServerData(serverData);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    connectionFailedEvent(e.Message);
                    return;
                }

                Singleton.StartClient();
            }
        }


        //if the IP address value passed in is empty, we will start a new LAN match as a host
        //otherwise, we will try to connect to an existing IP as a client
        void StartLAN(string targetIP)
        {
            bool isHost = string.IsNullOrEmpty(targetIP);

            if(isHost)
            {
                BroadcastData newData = new BroadcastData()
                {
                    HostName = PlayerPrefs.GetString(PrefsKeys.playerName),
                    MapName = "UNKNOWN",
                    CurrentPlayers = 1,
                    MaxPlayers = maxPlayers,
                    ConnectAddress = discovery.ipAddress
                };
                discovery.broadcastData = newData;

                currentLobby = new Lobby(
                    id: newData.ConnectAddress,
                    name: newData.HostName,
                    maxPlayers: newData.MaxPlayers,
                    availableSlots: newData.MaxPlayers - 1,
                    isPrivate: true,
                    data: new Dictionary<string, DataObject>()
                    {
                        {
                            "MapName", new DataObject(
                                visibility: DataObject.VisibilityOptions.Public,
                                value: newData.MapName,
                                index: DataObject.IndexOptions.S1)
                        },
                    });

                //set own IP so others can look it up
                (Singleton.NetworkConfig.NetworkTransport as UnityTransport).ConnectionData.Address = newData.ConnectAddress;
                Singleton.StartHost();
            }
            else
            {
                //connect to other host
                (Singleton.NetworkConfig.NetworkTransport as UnityTransport).ConnectionData.Address = targetIP;
                Singleton.StartClient();
            }
        }


        //start offline match as host without using any service
        void StartOffline()
        {
            Singleton.StartHost();
        }


        //the coroutine that pings the lobby every few seconds, so it stays alive
        //see SendHeartbeatPingAsync in the Unity documentation for more details
        IEnumerator HeartbeatLobbyCoroutine(string lobbyId, float waitTimeSeconds)
        {
            var delay = new WaitForSecondsRealtime(waitTimeSeconds);

            while (true)
            {
                LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
                yield return delay;
            }
        }


        /// <summary>
        /// Removes lobby from Unity Lobby (online) or stops broadcasting (LAN)
        /// so it cannot be joined anymore by other clients after it has been started.
        /// </summary>
        public static void RemoveLobby()
        {
            switch(instance.networkMode)
            {
                case NetworkMode.Online:
                    instance.StopCoroutine(instance.lobbyHeartbeatCoroutine);
                    LobbyService.Instance.DeleteLobbyAsync(instance.currentLobby.Id);
                    break;

                case NetworkMode.LAN:
                    instance.discovery.Stop();
                    break;
            }
        }


        /// <summary>
        /// Upon starting a server from the Intro scene it will load the Lobby scene.
        /// </summary>
        public void OnStartServer()
        {
            SceneManager.LoadScene(lobbyScene, LoadSceneMode.Single);
        }


        /// <summary>
        /// Override for the callback received on the server from INetworkPrefabInstanceHandler when a player object was disconnected. 
        /// Tries to destroy the player object.
        /// </summary>
        public void OnClientDisconnect(NetworkObject net)
        {
            if (net == null || !net.IsPlayerObject) return;

            //remove player
            try
            {
                net.Despawn(true);
                Destroy(net.gameObject);
            }
            catch (Exception) { }
        }


        /// <summary>
        /// Override for the callback received when a client was disconnected.
        /// Handles approval declines, connection not found errors and then transitions to starting scene.
        /// </summary>
        public void OnClientDisconnect(ulong clientId)
        {
            //approval was rejected when connecting
            //we return here so the connection approach runs into the UI timeout soon
            if (!IsServer && DisconnectReason != string.Empty)
            {
                Debug.LogWarning("Connection Approval Declined, Reason: " + DisconnectReason);
                connectionFailedEvent?.Invoke(DisconnectReason);
                return;
            }

            //still disconnected when this is called, so the host is not reachable or takes too long to answer
            if (!IsConnectedClient && clientId == 0)
            {
                Debug.Log("Timeout: did not find any matches on the Master Client we are connecting to.");
                Shutdown();
                return;
            }

            //skip further execution if we are not the disconnected client (received on server)
            if (IsServer && Singleton.LocalClientId != clientId)
            {
                return;
            }

            //do not switch scenes automatically when the game over screen is being shown already
            if (GameManager.GetInstance() != null && GameManager.GetInstance().IsGameOver())
                return;

            //clear assigned lobby, if present
            currentLobby = null;
            //switch from any other scene to the starting scene after connection is closed
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }


        /// <summary>
        /// Constructs the JoinMessage for the client by reading its device settings.
        /// </summary>
        public static byte[] GetJoinMessage()
        {
            //currently we only make use of the player name
            JoinMessage message = new JoinMessage();
            message.playerName = PlayerPrefs.GetString(PrefsKeys.playerName);

            using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
            {
                System.Xml.Serialization.XmlSerializer Serializer = new System.Xml.Serialization.XmlSerializer(typeof(JoinMessage));

                Serializer.Serialize(stream, message);
                return stream.ToArray();
            }
        }


        /// <summary>
        /// Implementation required when using INetworkPrefabInstanceHandler but we do nothing here.
        /// </summary>
        public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Implementation from INetworkPrefabInstanceHandler monitoring PlayerObject disconnects.
        /// </summary>
        public void Destroy(NetworkObject networkObject)
        {
            OnClientDisconnect(networkObject);
        }


        //unregister callbacks
        void OnDestroy()
        {
            OnServerStarted -= OnStartServer;
            ConnectionApprovalCallback -= ApprovalCheck;
            OnClientDisconnectCallback -= OnClientDisconnect;
        }
    }


    /// <summary>
    /// Network Mode selection for preferred network type.
    /// </summary>
    public enum NetworkMode
    {
        /// <summary>
        /// Online via Unity Relay and Lobby.
        /// </summary>
        Online,

        /// <summary>
        /// Broadcasting and detecting matches on Local Area Network.
        /// </summary>
        LAN,

        /// <summary>
        /// Offline, playing solo without internet connection.
        /// </summary>
        Offline
    }
    
    
    /// <summary>
    /// The client message constructed for the add player request to the server.
    /// You can extend this class to send more data at the point of joining a match.
    /// </summary>
    [Serializable]
    public struct JoinMessage
    {       
        /// <summary>
        /// The user name entered by the player in the game settings.
        /// </summary>
        public string playerName;
    }
}
