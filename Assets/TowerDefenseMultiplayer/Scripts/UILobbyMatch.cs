/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Lobbies.Models;

namespace TowerDefenseMP
{
    /// <summary>
    /// An match entry displayed in the Lobby overview, if public, so players can join it.
    /// </summary>
    public class UILobbyMatch : MonoBehaviour
    {
        /// <summary>
        /// Name of the lobby, usually player name.
        /// </summary>
        public Text lobbyName;

        /// <summary>
        /// Name of the map chosen for the match.
        /// </summary>
        public Text mapName;

        /// <summary>
        /// Current count of players in the match lobby.
        /// </summary>
        public Text playerCount;

        //internal lobby ID created by the Unity Lobby service
        private string lobbyId = string.Empty;
		       

        /// <summary>
        /// Initializes this script with data from a Lobby response.
        /// </summary>
        public void Initialize(Lobby lobby)
        {
            lobbyName.text = lobby.Name;
            mapName.text = lobby.Data["MapName"].Value;
            playerCount.text = (lobby.MaxPlayers - lobby.AvailableSlots) + " / " + lobby.MaxPlayers;

            lobbyId = lobby.Id;
        }


        /// <summary>
        /// Method for joining the match lobby, hooked to the entry's join button in the UI.
        /// </summary>
        public void Join()
        {               
            NetworkManagerCustom.StartMatch(lobbyId);
        }
    }
}