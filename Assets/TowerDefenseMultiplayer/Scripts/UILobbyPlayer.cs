/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Collections;

namespace TowerDefenseMP
{
    /// <summary>
    /// Networked representation of a player while in the Lobby scene,
    /// where they can select e.g. their class, color / location and ready flag.
    /// This is a server-authoritative component so each local action has a ServerRpc counterpart.
    /// </summary>
    public class UILobbyPlayer : NetworkBehaviour
    {
        /// <summary>
        /// Name of the player. This is set by the player's join message in UILobby.
        /// </summary>
        [HideInInspector]
        public NetworkVariable<FixedString32Bytes> myName = new NetworkVariable<FixedString32Bytes>();
		
		/// <summary>
        /// Class selected by the player in a UI dropdown.
        /// </summary>
        [HideInInspector]
        public NetworkVariable<int> myClass = new NetworkVariable<int>(0);
		
		/// <summary>
        /// Color selected by the player via a color cycle button.
        /// </summary>
        [HideInInspector]
        public NetworkVariable<int> myColor = new NetworkVariable<int>(0);
		
		/// <summary>
        /// Flag set by the player disabling all further input.
        /// </summary>
        [HideInInspector]
        public NetworkVariable<bool> myReady = new NetworkVariable<bool>(false);

        /// <summary>
        /// Image visualizing the active local player that allows input.
        /// </summary>
        public Image selectionImage;

        /// <summary>
        /// Text field for displaying the player's name.
        /// </summary>
        public Text nameText;
		
		/// <summary>
        /// Dropdown reference for selecting the class.
        /// </summary>
        public Dropdown classDropdown;
		
		/// <summary>
        /// Button reference for cycling through all colors.
        /// </summary>
        public Button colorButton;
		
		/// <summary>
        /// Toggle reference for setting the ready flag.
        /// </summary>
        public Toggle readyToggle;


        //hook into networked variable changes
        void Awake()
        {
            myName.OnValueChanged += OnChangeName;
            myClass.OnValueChanged += OnChangeClass;
            myColor.OnValueChanged += OnChangeColor;
            myReady.OnValueChanged += OnChangeReady;
        }


        //ensure RectTransform scale is set to 1, only Unity knows why. For some strange reason
        //when instantiating the prefab the scale is not 1/1/1 so we need to fix it
        IEnumerator Start()
        {
            yield return new WaitForSeconds(0.1f);

            RectTransform rectTrans = GetComponent<RectTransform>();
            rectTrans.localScale = Vector3.one;
        }


        /// <summary>
        /// Initialize synced values on every client.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            //call hooks manually to update
            OnChangeName("", myName.Value);
            OnChangeClass(0, myClass.Value);
            OnChangeColor(0, myColor.Value);
            OnChangeReady(false, myReady.Value);

            //also populate class dropdown with available class names
            classDropdown.AddOptions(GameData.GetInstance().GetClassNames());

            //only allow input for local player
            if (!IsLocalPlayer) return;
            selectionImage.enabled = true;
            classDropdown.interactable = true;
            colorButton.interactable = true;
            readyToggle.interactable = true;
        }


        /// <summary>
        /// Returns the player index based on the color that was selected.
        /// </summary>
        public int GetIndex()
        {
            return myColor.Value;
        }


        /// <summary>
        /// Update class value and inform other clients.
        /// </summary>
        public void ChangeClass(int newValue)
        {
            if (!IsLocalPlayer) return;

            ChangeClassServerRpc(newValue);
        }


        /// <summary>
        /// Update color value and inform other clients.
        /// </summary>
        public void ChangeColor()
        {
            if (!IsLocalPlayer) return;

            int nextColor = myColor.Value + 1;
            int count = Enum.GetNames(typeof(ClassColor)).Length;
            if (nextColor >= count) nextColor = 0;

            ChangeColorServerRpc(nextColor);
        }


        /// <summary>
        /// Update ready flag and inform other clients.
        /// </summary>
        public void ChangeReady(bool newValue)
        {
            if (!IsLocalPlayer) return;

            classDropdown.interactable = !newValue;
            colorButton.interactable = !newValue;

            ChangeReadyServerRpc(newValue);
        }


        /// <summary>
        /// Server updates NetworkVariable upon receiving a class change from a client.
        /// </summary>
        [ServerRpc]
        void ChangeClassServerRpc(int newValue)
        {
            myClass.Value = newValue;
        }


        /// <summary>
        /// Server updates NetworkVariable upon receiving a color change from a client.
        /// </summary>
        [ServerRpc]
        void ChangeColorServerRpc(int newValue)
        {
            foreach(NetworkClient client in NetworkManager.ConnectedClients.Values)
            {
                UILobbyPlayer player = client.PlayerObject.GetComponent<UILobbyPlayer>();
                if(player != this && player.myColor.Value == newValue)
                {
                    newValue++;
                }
            }

            myColor.Value = newValue;
        }


        /// <summary>
        /// Server updates NetworkVariable upon receiving a ready change from a client.
        /// </summary>
        [ServerRpc]
        void ChangeReadyServerRpc(bool newValue)
        {
            myReady.Value = newValue;
        }


        //hook for updating name locally
        protected void OnChangeName(FixedString32Bytes oldValue, FixedString32Bytes newValue)
        {
            nameText.text = newValue.Value;
        }


        //hook for updating class locally
        protected void OnChangeClass(int oldValue, int newValue)
        {
            classDropdown.value = newValue;
        }


        //hook for updating color locally
        protected void OnChangeColor(int oldValue, int newValue)
        {
            Color newColor;
            if(ColorUtility.TryParseHtmlString(((ClassColor)newValue).ToString(), out newColor))
            {
                colorButton.targetGraphic.color = newColor;
            }
        }


        //hook for updating ready locally
        protected void OnChangeReady(bool oldValue, bool newValue)
        {
            readyToggle.isOn = newValue;
        }
    }


    /// <summary>
    /// Enumeration of color definitions for selection.
    /// The name needs to match a color property.
    /// </summary>
    public enum ClassColor
    {
        BLACK = 0,
        GREEN = 1,
        BLUE = 2,
        ORANGE = 3,
        RED = 4,
        YELLOW = 5,
        AQUA = 6,
        MAGENTA = 7
    }


    /// <summary>
    /// Custom struct used for storing Player data across the network.
    /// </summary>
    [System.Serializable]
    public struct PlayerStruct : INetworkSerializable
    {
        /// <summary>
        /// The client Id that is the owner of this data.
        /// </summary>
        public ulong ownerId;

        /// <summary>
        /// UILobbyPlayer myName value.
        /// </summary>
        public FixedString32Bytes thisName;

        /// <summary>
        /// UILobbyPlayer myClass value.
        /// </summary>
        public int thisClass;

        /// <summary>
        /// UILobbyPlayer myColor value.
        /// </summary>
        public int thisColor;


        /// <summary>
        /// Constructor taking over the values from UILobbyPlayer.
        /// </summary>
        public PlayerStruct(UILobbyPlayer lobbyPlayer)
        {
            ownerId = lobbyPlayer.OwnerClientId;
            thisName = lobbyPlayer.myName.Value;
            thisClass = lobbyPlayer.myClass.Value;
            thisColor = lobbyPlayer.myColor.Value;
        }


        /// <summary>
        /// Serialize Values into network.
        /// </summary>
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ownerId);
            serializer.SerializeValue(ref thisName);
            serializer.SerializeValue(ref thisClass);
            serializer.SerializeValue(ref thisColor);
        }
    }
}