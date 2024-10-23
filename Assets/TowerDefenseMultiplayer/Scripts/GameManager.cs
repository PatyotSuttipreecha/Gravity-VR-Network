/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using System.Collections;
using UnityEngine;
using Unity.Netcode;

#if UNITY_ADS
using UnityEngine.Advertisements;
#endif

namespace TowerDefenseMP
{
    /// <summary>
    /// Manages game workflow and provides high-level access to networked logic during a game.
    /// It manages functions such as spawning players, currency, score and ending a game.
    /// </summary>
	public class GameManager : NetworkBehaviour
    {
        //reference to this script instance
        private static GameManager instance;

        /// <summary>
        /// The local player instance spawned for this client.
        /// </summary>
        [HideInInspector]
        public PlayerStruct localPlayer;

        /// <summary>
        /// Currency each player should start with at the beginning.
        /// </summary>
        public int startCurrency = 50;


        /// <summary>
        /// Location of player spawn points.
        /// </summary>
        public Transform[] playerSpawns;

        /// <summary>
        /// Networked list storing currency for each player.
        /// </summary>
        public NetworkList<int> currency;

        /// <summary>
        /// Networked list storing score for each player.
        /// </summary>
        public NetworkList<int> score;

        //whether the game is over
        private bool isGameOver = false;


        //initialize variables
        void Awake()
        {
            instance = this;

            currency = new NetworkList<int>();
            score = new NetworkList<int>();

            //set all player location icon colors locally
            for (int i = 0; i < playerSpawns.Length; i++)
            {
                Color targetColor;
                if (ColorUtility.TryParseHtmlString(((ClassColor)i).ToString(), out targetColor))
                    playerSpawns[i].GetComponentInChildren<Renderer>().material.color = targetColor;
            }
        }


        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static GameManager GetInstance()
        {
            return instance;
        }


        /// <summary>
        /// Once the server has finished loading the map and initialized the network,
        /// it initializes all NetworkLists i.e. currency and scores.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            //assign the local player data that this client owns
            foreach (PlayerStruct playerStruct in NetworkManagerCustom.GetInstance().playerData)
            {
                if(playerStruct.ownerId == NetworkManager.Singleton.LocalClientId)
                {
                    localPlayer = playerStruct;
                    break;
                }
            }

            if (IsServer)
            {
                int playerCount = NetworkManagerCustom.GetInstance().maxPlayers;
                for (int i = 0; i < playerCount; i++)
                {
                    currency.Add(startCurrency);
                    score.Add(0);
                }
            }
        }


        /// <summary>
        /// Adds currency to the player upon defeating a Unit.
        /// </summary>
        public void AddCurrency(int playerIndex, int points)
        {
            //convert from client Id to color position
            currency[NetworkManagerCustom.GetInstance().playerData[playerIndex].thisColor] += points;
        }


        /// <summary>
        /// Adds one to score of a player upon defeating a Unit.
        /// </summary>
        public void AddScore(int playerIndex)
        {
            //convert from client Id to color position
            score[NetworkManagerCustom.GetInstance().playerData[playerIndex].thisColor] += 1;
        }


        /// <summary>
        /// Returns whether the game is over or not.
        /// </summary>
        public bool IsGameOver()
        {
            return isGameOver;
        }


        /// <summary>
        /// Global check whether this client is the match master or not.
        /// </summary>
        public static bool IsMaster()
        {
            return NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost;
        }


        /// <summary>
        /// The local player receives a request from the server to finish the game.
        /// The variable describes whether the match was won or lost. Disables player movement
        /// by setting the timescale to zero so no input is processed anymore.
        /// </summary>
        [ClientRpc]
        public void GameOverClientRpc(bool success)
        {
            isGameOver = true;
            Time.timeScale = 0;
            UIManager.GetInstance().ui.SetGameOverText(success);

            #if UNITY_ADS
            UnityAdsManager.ShowGameEndAd();
            #endif

            StartCoroutine(DisplayGameOver());
        }


        //displays game over window after short delay
        IEnumerator DisplayGameOver()
        {
            //give the user a chance to read whether they lost or won
            //before enabling the game over screen
            yield return new WaitForSecondsRealtime(3);

            //show game over window and disconnect from network
            UIManager.GetInstance().ui.ShowGameOver();
            yield return new WaitForSecondsRealtime(1);
            NetworkManager.Singleton.Shutdown();
        }


        //draw some gizmos for the player spawn location in the editor
        #if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1, 0.92f, 0.16f, 0.2f);

            for (int i = 0; i < playerSpawns.Length; i++)
            {
                Vector3 pos = playerSpawns[i].position + new Vector3(0, 0.5f, 0);

                Gizmos.DrawCube(pos, Vector3.one);
                UnityEditor.Handles.Label(pos, "Player " + i);
            }
        }
        #endif
    }
}
