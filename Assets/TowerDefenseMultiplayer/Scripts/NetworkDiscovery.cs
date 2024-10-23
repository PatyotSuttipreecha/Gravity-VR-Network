/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies.Models;
using System.Threading.Tasks;

namespace TowerDefenseMP
{
    /// <summary>
    /// This component allows finding each other on a local network. It can broadcast presence and listen for broadcasts, and optionally join matching games using the NetworkManager.
    /// This component can run in server mode (by calling StartSendThread) where it broadcasts to other computers on the local network, or in client mode (by calling StartListenThread) where it listens for broadcasts from a server.
    /// Unity had a NetworkDiscovery component in the past, but that does not work with Netcode anymore so this was fully rewritten using raw UdpClient messages.
    /// </summary>
    [DisallowMultipleComponent]
    public class NetworkDiscovery : MonoBehaviour
    {
        //queue of broadcast messages retrieved worked on in the Unity main thread
        private readonly Queue<string> matchQueue = new Queue<string>();

        //udp reference used for both sending and listening
        private UdpClient udpClient;
        //the target broadcast address where the server should send, or clients listen to
        private string broadcastAddress;
        //the target broadcast port, taken from NetworkManager's NetworkTransport with 10 added
        private int broadcastPort;
        //a variable caching the server's IP address clients later connect to
        [HideInInspector]
        public string ipAddress;
        //the data the broadcast message should contain
        [HideInInspector]
        public BroadcastData broadcastData = new BroadcastData();

        //a thread variable for both sending and listening
        private Thread sendThread;
        private Thread listenThread;
        //a boolean indiciating whether threads are active
        private bool sendRunning = false;
        private bool listenRunning = false;


        //initialize UDP client connection
        void Start()
        {
            //get own IP Address and replace last number range with 255, so it looks like e.g. 192.168.178.255
            //using 255.255.255.255 as broadcast address causes issues on Mac ignoring the message so that cannot be used
            ipAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(f => f.AddressFamily == AddressFamily.InterNetwork).ToString();
            string[] ipParts = ipAddress.Split('.');
            ipParts[3] = "255";

            //reconstruct broadcast address and get port from NetworkTransport
            broadcastAddress = string.Join(".", ipParts);
            broadcastPort = (NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport).ConnectionData.Port + 10;

            //create udp client and bind to broadcast port
            try
            {
                udpClient = new UdpClient(broadcastPort);
                udpClient.EnableBroadcast = true;
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to create UdpClient at port " + broadcastPort + ": " + e.Message);
            }
        }


        /// <summary>
        /// Called on a server to start sending messages.
        /// </summary>
        public void StartSendThread()
        {
            Stop();

            //create sending thread
            sendThread = new Thread(() => BroadcastMessages());
            sendThread.IsBackground = true;
            sendRunning = true;
            sendThread.Start();
        }


        //called by StartSendThread()
        void BroadcastMessages()
        {
            //create destination endpoint for the message
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(broadcastAddress), broadcastPort);
            
            while (sendRunning)
            {
                try
                {
                    //convert broadcast data content into json for easy transmission
                    string json = JsonUtility.ToJson(broadcastData);
                    byte[] bytes = Encoding.UTF8.GetBytes(json);

                    udpClient.Send(bytes, bytes.Length, endpoint);

                    //Debug.Log("Server sent: " + json);
                }
                catch (Exception e)
                {
                    Debug.LogError("Error sending data: " + (e != null ? e.Message : "No further information."));
                }

                //wait a bit for another send
                Thread.Sleep(2000);
            }
        }


        /// <summary>
        /// Called on a client to start listening for messages.
        /// </summary>
        public async Task<List<Lobby>> StartListenThread()
        {
            //create listening thread
            listenThread = new Thread(() => ListenForMessages());
            listenThread.IsBackground = true;
            listenRunning = true;
            listenThread.Start();

            return await Task.Run(() => {
                Thread.Sleep(2000);
                listenRunning = false;
                listenThread.Abort();
                return ProcessMatchQueue();
            });
        }


        //called by StartListenThread()
        void ListenForMessages()
        {
            //create receiving endpoint for the message
            //we use IPAddress.Any here to listen for messages on sent to all addresses
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(broadcastAddress), broadcastPort);

            while (listenRunning)
            {
                try
                {
                    //try to read message from endpoint
                    byte[] receiveBytes = udpClient.Receive(ref endPoint);
                    string json = Encoding.UTF8.GetString(receiveBytes);

                    if (!matchQueue.Contains(json))
                    {
                        lock (matchQueue)
                        {
                            matchQueue.Enqueue(json);
                        }

                        //Debug.Log("Client received: " + json);
                    }
                }
                catch (SocketException e)
                {
                    //10004 = socket closed
                    if (e.ErrorCode != 10004)
                        Debug.LogError("Socket exception on listen: " + e.Message);
                }
                catch (Exception e)
                {
                    if (udpClient == null)
                    {
                        Debug.LogError("Failed to create UdpClient. Are you testing on two different devices?");
                        return;
                    }

                    if (!string.IsNullOrEmpty(e.Message) && e.Message != "Thread was being aborted.")
                        Debug.LogError("Error receiving data: " + e.Message);
                }

                //wait a bit for another listen
                Thread.Sleep(100);
            }
        }


        //we need to process received broadcast data in a separate queue on the main thread,
        //because the listen task has its own thread and you cannot use Unity code in there
        List<Lobby> ProcessMatchQueue()
        {
            string queueItem = string.Empty;
            BroadcastData matchData;
            List<Lobby> lobbyList = new List<Lobby>();

            lock (matchQueue)
            {
                while (matchQueue.Count != 0)
                {
                    //read match entry from the queue
                    queueItem = matchQueue.Dequeue();
                    matchData = JsonUtility.FromJson<BroadcastData>(queueItem);

                    if(matchData.ConnectAddress == ipAddress || matchData.CurrentPlayers == matchData.MaxPlayers)
                    {
                        continue;
                    }

                    lobbyList.Add(new Lobby(
                                    id: matchData.ConnectAddress,
                                    name: matchData.HostName,
                                    maxPlayers: matchData.MaxPlayers,
                                    availableSlots: (matchData.MaxPlayers - matchData.CurrentPlayers),
                                    data: new Dictionary<string, DataObject>()
                                    {
                                        { "MapName", new DataObject(DataObject.VisibilityOptions.Public, matchData.MapName) }
                                    }));
                }
            }

            return lobbyList;
        }


        /// <summary>
        /// Terminate threads and shut down UdpClient.
        /// </summary>
        public void Stop()
        {
            if (sendRunning)
            {
                sendRunning = false;
                sendThread.Abort();
            }

            if (listenRunning)
            {
                listenRunning = false;
                listenThread.Abort();
                matchQueue.Clear();
            }
        }


        //stop and throw away UDPClient
        void CleanUp()
        {
            Stop();

            if (udpClient != null)
            {
                udpClient.Close();
                udpClient.Dispose();
            }
        }


        void OnDestroy()
        {
            CleanUp();
        }


        void OnApplicationQuit()
        {
            CleanUp();
        }
    }


    /// <summary>
    /// Struct storing data that should be exchanged between server and clients.
    /// </summary>
    public struct BroadcastData : INetworkSerializable
    {
        /// <summary>
        /// Player name hosting the match.
        /// </summary>
        public string HostName;

        /// <summary>
        /// Map name that is set in the match.
        /// </summary>
        public string MapName;

        /// <summary>
        /// IP address of the host that is broadcasting the data.
        /// </summary>
        public string ConnectAddress;

        /// <summary>
        /// Count of players that have joined the match so far.
        /// </summary>
        public int CurrentPlayers;

        /// <summary>
        /// Maximum count of players that are allowed to join the match.
        /// </summary>
        public int MaxPlayers;


        /// <summary>
        /// Serializer method that sets references to all data that should be included in the broadcast.
        /// </summary>
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref HostName);
            serializer.SerializeValue(ref MapName);
            serializer.SerializeValue(ref ConnectAddress);
            serializer.SerializeValue(ref CurrentPlayers);
            serializer.SerializeValue(ref MaxPlayers);
        }
    }
}