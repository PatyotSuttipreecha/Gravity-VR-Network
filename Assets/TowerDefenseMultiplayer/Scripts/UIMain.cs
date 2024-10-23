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
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;

namespace TowerDefenseMP
{
    /// <summary>
    /// UI script for all elements, settings and user interactions in the Intro scene.
    /// Provides access to network mode selection and creating or joining a lobby.
    /// </summary>
    public class UIMain : MonoBehaviour
    {
        /// <summary>
        /// Separate window for toggling display of the available lobby matches in online or LAN.
        /// </summary>
        public GameObject lobbyListWindow;

        /// <summary>
        /// Container transform under which the lobby match prefabs should be instantiated.
        /// </summary>
        public Transform lobbyListContainer;

        /// <summary>
        /// The match prefab that represents a single match in the lobby list.
        /// </summary>
        public GameObject lobbyListEntryPrefab;

        /// <summary>
        /// Input for the code that also allows joining a private match on the Unity Lobby service.
        /// </summary>
        public InputField lobbyCodeField;

        /// <summary>
        /// List of lobby matches that were retrieved by the NetworkManagerCustom lobbyListUpdateEvent.
        /// </summary>
        [HideInInspector]
        public List<UILobbyMatch> lobbyListEntries = new List<UILobbyMatch>();

        /// <summary>
        /// Window object for loading screen between connecting and scene switch.
        /// </summary>
        public GameObject loadingWindow;

        /// <summary>
        /// Window object for displaying errors with the connection or timeouts.
        /// </summary>
        public GameObject connectionErrorWindow;
        
        /// <summary>
        /// Window object for displaying errors with the billing actions.
        /// </summary>
        public GameObject billingErrorWindow;
        
        /// <summary>
		/// Settings: input field for the player name.
		/// </summary>
		public InputField nameField;

        /// <summary>
        /// Settings: dropdown selection for network mode.
        /// </summary>
        public Dropdown networkDrop;

 		/// <summary>
		/// Settings: checkbox for playing background music.
		/// </summary>
		public Toggle musicToggle;

		/// <summary>
		/// Settings: slider for adjusting game sound volume.
		/// </summary>
		public Slider volumeSlider;


        //initialize player selection in Settings window
        //if this is the first time launching the game, set initial values
        void Start()
        {      
            //set initial values for all settings         
            if (!PlayerPrefs.HasKey(PrefsKeys.playerName)) PlayerPrefs.SetString(PrefsKeys.playerName, "User" + string.Format("{0:0000}", UnityEngine.Random.Range(1, 9999)));
            if (!PlayerPrefs.HasKey(PrefsKeys.networkMode)) PlayerPrefs.SetInt(PrefsKeys.networkMode, 0);
            if (!PlayerPrefs.HasKey(PrefsKeys.playMusic)) PlayerPrefs.SetString(PrefsKeys.playMusic, "true");
            if (!PlayerPrefs.HasKey(PrefsKeys.appVolume)) PlayerPrefs.SetFloat(PrefsKeys.appVolume, 1f);
            PlayerPrefs.Save();

            //backup network mode for accessing it later
            NetworkManagerCustom.GetInstance().networkMode = (NetworkMode)PlayerPrefs.GetInt(PrefsKeys.networkMode);

            //read the selections and set them in the corresponding UI elements
            nameField.text = PlayerPrefs.GetString(PrefsKeys.playerName);
            networkDrop.value = PlayerPrefs.GetInt(PrefsKeys.networkMode);
            musicToggle.isOn = bool.Parse(PlayerPrefs.GetString(PrefsKeys.playMusic));
            volumeSlider.value = PlayerPrefs.GetFloat(PrefsKeys.appVolume);

            //call the onValueChanged callbacks once with their saved values
            OnMusicChanged(musicToggle.isOn);
            OnVolumeChanged(volumeSlider.value);
         
            //listen to relevant error callbacks
            UnityIAPManager.purchaseFailedEvent += OnBillingError;
            NetworkManagerCustom.lobbyListUpdateEvent += OnLobbyListUpdate;
            NetworkManagerCustom.connectionFailedEvent += OnConnectionError;
        }


        /// <summary>
        /// Most important method in this script - request available lobbies from NetworkManagerCustom.
        /// Displays them in the lobbyListWindow via callback, or creates a game directly when in offline mode.
        /// </summary>
        public async void Play()
        {
            //depending on the selected network mode
            switch(NetworkManagerCustom.GetInstance().networkMode)
            {
                //for accessing Unity Relay and Lobby online, the player needs to be authenticated
                //here the AuthentificationService part for that is handled and then lobbies requested
                case NetworkMode.Online:

                    loadingWindow.SetActive(true);
                    try
                    {
                        string playerName = PlayerPrefs.GetString(PrefsKeys.playerName);

                        if(UnityServices.State == ServicesInitializationState.Uninitialized)
                        {
                            InitializationOptions initializationOptions = new InitializationOptions();
                            initializationOptions.SetProfile(playerName);
                            await UnityServices.InitializeAsync(initializationOptions);
                        }

                        if(AuthenticationService.Instance.IsSignedIn && AuthenticationService.Instance.Profile != playerName)
                        {
                            AuthenticationService.Instance.SignOut();
                            AuthenticationService.Instance.SwitchProfile(playerName);
                        }

                        if(!AuthenticationService.Instance.IsSignedIn)
                            await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.Message);

                        //display connection issue window
                        loadingWindow.SetActive(false);
                        OnConnectionError(e.Message);
                        break;
                    }
                    
                    QueryLobbies();
                    break;

                //in LAN no authentification is necessary and also we do want to display the lobbyListWindow directly
                //so this means no loading screen and then fill it up with available lobby matches later (if any)
                case NetworkMode.LAN:
                    lobbyListWindow.SetActive(true);
                    QueryLobbies();
                    break;

                //no service code at all, just create our offline lobby directly
                case NetworkMode.Offline:
                    loadingWindow.SetActive(true);
                    NetworkManagerCustom.StartMatch(null);
                    break;
            }
        }


        /// <summary>
        /// Mapped to the Host button in the UI. Regardless of whether lobbies have been found
        /// or not, the player can create its own lobby by using this button.
        /// </summary>
        public void PlayHost()
        {
            loadingWindow.SetActive(true);
            NetworkManagerCustom.StartMatch(null);
            StartCoroutine(HandleTimeout());
        }


        /// <summary>
        /// Mapped to the Join button in the UI. Using the lobby code,
        /// the player can join a private (or public) lobby using this button.
        /// </summary>
        public void PlayPrivate()
        {
            loadingWindow.SetActive(true);
            NetworkManagerCustom.StartMatch(lobbyCodeField.text);
            StartCoroutine(HandleTimeout());
        }


        //coroutine that waits x seconds before cancelling joining a match
        IEnumerator HandleTimeout()
        {
            //in case we are still not connected after the timeout happened, display the error window
            yield return new WaitForSeconds(10);

            //timeout has passed, we would like to stop joining a game now
            NetworkManager.Singleton.Shutdown();
            (NetworkManager.Singleton as NetworkManagerCustom).OnClientDisconnect(0);

            //display connection issue window
            OnConnectionError();
        }


        /// <summary>
        /// Initiates a call to NetworkManagerCustom for requesting current lobbies from the service.
        /// </summary>
        public void QueryLobbies()
        {
            NetworkManagerCustom.QueryLobbies();
        }


        //after the lobby list response has been received, update the displayed list
        //this instantiates new entries but also deletes old ones depending on received count
        void OnLobbyListUpdate(List<Lobby> list)
        {
            //show list of matches
            if(loadingWindow.activeInHierarchy)
            {
                loadingWindow.SetActive(false);
                lobbyListWindow.SetActive(true);
            }

            //delete entries if there are more than we need now
            for (int i = lobbyListEntries.Count; i > list.Count; i--)
            {
                Destroy(lobbyListEntries[i - 1].gameObject);
                lobbyListEntries.RemoveAt(i - 1);
            }

            //add more entries if the response is greater than current count
            for (int i = lobbyListEntries.Count; i < list.Count; i++)
            {
                GameObject newEntry = Instantiate(lobbyListEntryPrefab);
                newEntry.transform.SetParent(lobbyListContainer, false);
                lobbyListEntries.Add(newEntry.GetComponent<UILobbyMatch>());
            }

            //initialize or overwrite each entry with match data received
            for(int i = 0; i < list.Count; i++)
            {
                lobbyListEntries[i].Initialize(list[i]);
            }

            //call a resposition to force transform into grid layout
            lobbyListContainer.gameObject.GetComponent<UIGridAutoReposition>().Reposition();
        }


        //activates the connection error window to be visible
        void OnConnectionError(string error = null)
        {
            if(error == null)
                error = "Connection failed.\nPlease try again.";

            //get text label to display connection failed reason
            Text errorLabel = connectionErrorWindow.GetComponentInChildren<Text>();
            if (errorLabel)
                errorLabel.text = "Connection failed.\n\n" + error;

            StopCoroutine(HandleTimeout());
            loadingWindow.SetActive(false);
            connectionErrorWindow.SetActive(true);
        }
        
        
        //activates the billing error window to be visible
        void OnBillingError(string error)
        {
            //get text label to display billing failed reason
            Text errorLabel = billingErrorWindow.GetComponentInChildren<Text>();
            if(errorLabel)
                errorLabel.text = "Purchase failed.\n\n" + error;
            
            billingErrorWindow.SetActive(true);
        }

		
        /// <summary>
        /// Modify music AudioSource based on player selection.
        /// Called by Toggle onValueChanged event.
        /// </summary>
        public void OnMusicChanged(bool value)
        {
			AudioManager.GetInstance().musicSource.enabled = musicToggle.isOn;
            AudioManager.PlayMusic(0);
        }


        /// <summary>
        /// Modify global game volume based on player selection.
        /// Called by Slider onValueChanged event.
        /// </summary>
        public void OnVolumeChanged(float value)
        {
            volumeSlider.value = value;
            AudioListener.volume = value;
        }
			

        /// <summary>
        /// Saves all player selections chosen in the Settings window on the device.
        /// </summary>
        public void CloseSettings()
        {
            PlayerPrefs.SetString(PrefsKeys.playerName, nameField.text);
            PlayerPrefs.SetInt(PrefsKeys.networkMode, networkDrop.value);
            PlayerPrefs.SetString(PrefsKeys.playMusic, musicToggle.isOn.ToString());
            PlayerPrefs.SetFloat(PrefsKeys.appVolume, volumeSlider.value);
            PlayerPrefs.Save();

            NetworkManagerCustom.GetInstance().networkMode = (NetworkMode)PlayerPrefs.GetInt(PrefsKeys.networkMode);
        }

			
        /// <summary>
        /// Opens a browser window to the App Store entry for this app.
        /// </summary>
        public void RateApp()
        {           
            //default app url on non-mobile platforms
            //replace with your website, for example
			string url = "";
			
			#if UNITY_ANDROID
				url = "http://play.google.com/store/apps/details?id=" + Application.identifier;
			#elif UNITY_IPHONE
				url = "https://itunes.apple.com/app/idXXXXXXXXX";
			#endif
			
			if(string.IsNullOrEmpty(url) || url.EndsWith("XXXXXX"))
            {
                Debug.LogWarning("UIMain: You didn't replace your app links!");
                return;
            }
			
			Application.OpenURL(url);
        }


        //unsubscribe callbacks
        void OnDestroy()
        {
            UnityIAPManager.purchaseFailedEvent -= OnBillingError;
            NetworkManagerCustom.lobbyListUpdateEvent -= OnLobbyListUpdate;
            NetworkManagerCustom.connectionFailedEvent -= OnConnectionError;
        }
    }
}