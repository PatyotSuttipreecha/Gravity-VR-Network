/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Collections;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;

namespace TowerDefenseMP
{
    /// <summary>
    /// UI script for all elements, settings and user interactions in the Lobby scene.
    /// Handles networked messages like adding players and changing the selected map.
    /// </summary>
    public class UILobby : NetworkBehaviour
    {
        /// <summary>
        /// Name of the map synced to clients so they can change the map preview image too.
        /// </summary>
        public NetworkVariable<FixedString32Bytes> mapName = new NetworkVariable<FixedString32Bytes>();

        /// <summary>
        /// Container under which the player prefabs are getting instantiated in a row.
        /// </summary>
        public Transform playersParent;

        /// <summary>
        /// The player prefab representing a player in the lobby, with a UILobbyPlayer component.
        /// </summary>
        public GameObject playerPrefab;

        /// <summary>
        /// Reference to the button starting the match.
        /// </summary>
        public GameObject startButton;

        /// <summary>
        /// Reference to the dropdown presenting all available maps for selection.
        /// </summary>
        public Dropdown mapDropdown;

        /// <summary>
        /// Reference to the image displaying a preview of the selected map.
        /// </summary>
        public Image mapImage;

        /// <summary>
        /// Whether the match is set to public and therefore can be found by other players in the lobby list.
        /// </summary>
        public Toggle publicToggle;

        /// <summary>
        /// The code that is required when trying to join this match in a private state.
        /// </summary>
        public Text lobbyCodeText;

        /// <summary>
        /// Text that displays any errors or missing steps preventing the match to start.
        /// </summary>
        public Text errorText;


        //initialize callbacks
        void Awake()
        {
            SceneManager.sceneUnloaded += OnSceneUnload;

            mapName.OnValueChanged += OnChangeMap;
            publicToggle.onValueChanged.AddListener(delegate {
                OnChangePublic(publicToggle);
            });
        }


        /// <summary>
        /// After joining a lobby and initializing networked variables, update the game UI with their values,
        /// i.e. on the server allow for further input options, but for all clients add player and update map image.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            //set map to the first one available from dropdown
            //also if within a lobby (not offline) set the lobby code
            if(IsServer)
            {
                mapName.Value = mapDropdown.options[0].text;

                if(NetworkManagerCustom.GetInstance().currentLobby != null)
                {
                    lobbyCodeText.text = NetworkManagerCustom.GetInstance().currentLobby.LobbyCode;
                }
            }

            //allow further input on the server
            publicToggle.gameObject.SetActive(IsServer && NetworkManagerCustom.GetInstance().currentLobby != null);
            mapDropdown.gameObject.SetActive(IsServer);
            startButton.SetActive(IsServer);

            //call hooks manually to update
            OnChangeMap("", mapName.Value.Value);

            //have the local player representation be added to players list
            AddPlayerServerRpc(NetworkManagerCustom.GetJoinMessage());
        }


        //a client or the server itself requests creating its player prefab for displaying it in the lobby.
        [ServerRpc(RequireOwnership = false)]
        void AddPlayerServerRpc(byte[] connectionData, ServerRpcParams serverRpcParams = default)
        {
            //making sure this is server authoritative
            if (!IsServer)
                return;

            //read message data that was sent with the RPC
            JoinMessage message = new JoinMessage();
            using (System.IO.MemoryStream stream = new System.IO.MemoryStream(connectionData))
            {
                System.Xml.Serialization.XmlSerializer Serializer = new System.Xml.Serialization.XmlSerializer(typeof(JoinMessage));
                message = (JoinMessage)Serializer.Deserialize(stream);
            }

            //read the user message
            if (string.IsNullOrEmpty(message.playerName))
            {
                Debug.Log("AddPlayer called with empty player name!");
                return;
            }

            //find next color that has not been assigned to any player yet
            int startingColor = 0;
            foreach (NetworkClient client in NetworkManager.ConnectedClients.Values)
            {
                if (client.PlayerObject == null) continue;
                UILobbyPlayer otherPlayer = client.PlayerObject.GetComponent<UILobbyPlayer>();
                if (otherPlayer != null && otherPlayer.myColor.Value == startingColor)
                {
                    startingColor++;
                }
            }

            //instantiate player slot prefab
            GameObject playerObj = Instantiate(playerPrefab);
            UILobbyPlayer player = playerObj.GetComponent<UILobbyPlayer>();

            //finally map the player gameobject to the connection requesting it
            player.NetworkObject.SpawnAsPlayerObject(serverRpcParams.Receive.SenderClientId, true);
            playerObj.transform.SetParent(playersParent, false);

            //sync NetworkVariables after spawn
            player.myName.Value = message.playerName;
            player.myColor.Value = startingColor;
        }


        //sends updated data about the lobby to the Unity Lobby service
        //we do this every few seconds to always display the correct map in the lobby list
        IEnumerator UpdateLobbyData()
        {
            while (true)
            {
                //built-in lobby data, first making it public or private
                UpdateLobbyOptions options = new UpdateLobbyOptions();
                options.IsPrivate = !publicToggle.isOn;

                //custom lobby data
                options.Data = new Dictionary<string, DataObject>()
                {
                    {
                        "MapName", new DataObject(
                            visibility: DataObject.VisibilityOptions.Public,
                            value: mapName.Value.ToString(),
                            index: DataObject.IndexOptions.S1)
                    },
                };

                //only send an update if lobby visibility or map changed
                Lobby thisLobby = NetworkManagerCustom.GetInstance().currentLobby;
                if (thisLobby.IsPrivate != options.IsPrivate || thisLobby.Data["MapName"].Value != options.Data["MapName"].Value)
                {
                    NetworkManagerCustom.UpdateLobbyData(options);
                }

                //private matches do not need to update their public data
                if (options.IsPrivate == true)
                    yield break;

                yield return new WaitForSeconds(10);
            }
        }


        /// <summary>
        /// Starts the match and transitions all players into the selected map scene.
        /// Before that, checks if all players have their ready flag enabled.
        /// </summary>
        public void StartGame()
        {
            bool allReady = true;

            //loop over players to check their ready flag
            foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
            {
                UILobbyPlayer player = client.PlayerObject.GetComponent<UILobbyPlayer>();
                if (!player.myReady.Value)
                {
                    allReady = false;
                    break;
                }
            }

            //someone is not ready yet
            if(!allReady)
            {
                errorText.text = "Not all players ready yet.";
                return;
            }

            //reset list of players with their data, if it has been set before
            List<PlayerStruct> playerList = new List<PlayerStruct>();
            //once again loop over players but this time save and sync their data
            foreach (NetworkClient client in NetworkManager.ConnectedClientsList)
            {
                UILobbyPlayer player = client.PlayerObject.GetComponent<UILobbyPlayer>();
                PlayerStruct data = new PlayerStruct(player);
                playerList.Add(data);
            }

            //send update to clients with data they need to know before starting the map
            LoadMapClientRpc(playerList.ToArray());
        }


        //receive list of final player data from the server
        //let players see a map launch countdown
        [ClientRpc]
        void LoadMapClientRpc(PlayerStruct[] playerList)
        {
            NetworkManagerCustom.GetInstance().playerData = playerList;

            StartCoroutine(LoadMapDelayRoutine());
        }


        //displays the countdown value to everyone
        //on the server this also loads the actual map
        IEnumerator LoadMapDelayRoutine()
        {
            int delay = 3;
            while (delay != 0)
            {
                errorText.text = "Game starts in... " + delay;
                yield return new WaitForSeconds(1);
                delay--;
            }

            if (IsServer)
            {
                //let the server transition to playable map scene, clients will follow automatically
                NetworkManager.SceneManager.LoadScene(mapName.Value.Value, LoadSceneMode.Single);
            }
        }


        /// <summary>
        /// Called by Map dropdown in OnValueChanged (inspector) when selecting a new value on the server.
        /// </summary>
        public void ChangeMap(int value)
        {
            if (!IsServer) return;
            mapName.Value = mapDropdown.options[value].text;
        }


        //callback hook for updating selected map locally
        //the new value was received from the server that manages this NetworkVariable
        protected void OnChangeMap(FixedString32Bytes oldValue, FixedString32Bytes newValue)
        {
            //find map index based on map name received
            int index = -1;
            for (int i = 0; i < mapDropdown.options.Count; i++)
            {
                if(mapDropdown.options[i].text == newValue.Value)
                {
                    index = i;
                    break;
                }
            }

            //try to apply map or display error
            errorText.text = "";
            if (index >= 0) mapImage.sprite = mapDropdown.options[index].image;
            else errorText.text = "The selected map is not available in your build.";
        }


        //called by the public checkbox in the UI when values are changing
        //for public matches send updated data to Unity Lobby service every few seconds
        protected void OnChangePublic(Toggle toggle)
        {
            if (toggle.isOn)
            {
                StopAllCoroutines();
                StartCoroutine(UpdateLobbyData());

                if (NetworkManagerCustom.GetInstance().networkMode == NetworkMode.LAN)
                {
                    NetworkManagerCustom.GetInstance().discovery.StartSendThread();
                }
            }   
        }


        /// <summary>
        /// Stops receiving further network updates by hard disconnecting, then load starting scene.
        /// </summary>
        public void Disconnect()
        {
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene(0);
        }


        //we cannot use OnDestroy because its order in Unity is undefined, causing issues
        //when NetworkManagerCustom is destroyed prior to this script, resulting in NREs
        void OnSceneUnload(Scene scene)
        {
            SceneManager.sceneUnloaded -= OnSceneUnload;

            //remove lobby from Unity Lobby service
            if (IsServer)
            {
                NetworkManagerCustom.RemoveLobby();
            }
        }
    }
}