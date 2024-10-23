/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Unity.Netcode;

namespace TowerDefenseMP
{
    /// <summary>
    /// UI script for all elements, settings and user interactions in the Map scene(s).
    /// This is actually not part of the map scenes itself but the separate Map_UI scene.
    /// Handles all game UI like displaying tower buttons, currency, placing towers etc.
    /// </summary>
    public class UIGame : MonoBehaviour
    {
        [Header("Actions")]
        /// <summary>
        /// Container for buttons to buy a new tower.
        /// </summary>
        public GameObject buyActions;

        /// <summary>
        /// Container for buttons on an existing tower, like upgrade and sell.
        /// </summary>
        public GameObject upgradeActions;

        [Header("Buttons")]
        /// <summary>
        /// Prefab for an empty button that fills up the actions grid.
        /// </summary>
        public GameObject emptyButton;

        /// <summary>
        /// Prefab for a tower button that is initialized with tower data.
        /// </summary>
        public GameObject towerButton;

        [Header("Tooltip")]
        /// <summary>
        /// Tooltip component displaying information for a new tower.
        /// </summary>
        public UIBuyTooltip buyTooltip;

        /// <summary>
        /// Tooltip component displaying information for an existing tower.
        /// </summary>
        public UIUpgradeTooltip upgradeTooltip;

        [Header("Minimap")]
        /// <summary>
        /// Background image reference of the minimap giving an impression about the map layout.
        /// This is set by the UIManager per map, since it is not the same across maps.
        /// </summary>
        public RawImage mapImage;

        [Header("Info")]
        /// <summary>
        /// Text for displaying the current wave or time until the next wave starts.
        /// </summary>
        public Text waveText;

        /// <summary>
        /// Text for the local player's currency amount.
        /// </summary>
        public Text localCurrency;

        /// <summary>
        /// Text for the local player's score count.
        /// </summary>
        public Text localScore;

        /// <summary>
        /// Text for displaying the lowest defense point health value in percentage.
        /// </summary>
        public Text defenseSummary;

        /// <summary>
        /// Text that displays whether the match was won or lost.
        /// </summary>
        public Text gameOverText;

        [Header("Stats")]
        /// <summary>
        /// UI texts displaying each player's name.
        /// </summary>
        public Text[] playerNames;

        /// <summary>
        /// UI texts displaying currency for each player.
        /// </summary>
        public Text[] playerCurrency;

        /// <summary>
        /// UI texts displaying scores for each player.
        /// </summary>
        public Text[] playerScore;

        /// <summary>
        /// UI texts displaying remaining health for each player.
        /// </summary>
        public Text[] defenseHealth;

        [Header("Windows")]
        /// <summary>
        /// Window that is activated when the game ends, for both won or lost.
        /// </summary>
        public GameObject gameOverWindow;

        private int towerIndex;
        private GameObject towerFloating;
        private SelectMode selectMode = SelectMode.Tower;
        private GridTile hitTile = null;
        private Tower selectedTower = null;

        private Ray inputRay;
        private RaycastHit inputHit;


        //initialize callbacks
        void Awake()
        {
            WaveManager.wavePreEvent += OnWavePreEvent;
            TowerManager.changeTowerEvent += OnChangeTowerEvent;
        }


        //get the player class and instantiate all available tower buttons for that class
        IEnumerator Start()
        {
            //wait until the network is ready
            while (GameManager.GetInstance() == null || GameManager.GetInstance().localPlayer.Equals(default(PlayerStruct)))
                yield return null;

            //subscribe to NetworkVariable changes that should be reflected in the UI
            GameManager.GetInstance().currency.OnListChanged += OnPlayerCurrencyChanged;
            GameManager.GetInstance().score.OnListChanged += OnPlayerScoreChanged;
            DefensePointManager.GetInstance().health.OnListChanged += OnDefenseHealthChanged;

            //call the hooks manually with current values for the first time, for each player
            OnPlayerNameChanged();
            for (int i = 0; i < GameManager.GetInstance().score.Count; i++) OnPlayerScoreChanged(new NetworkListEvent<int>() { Index = i, Value = GameManager.GetInstance().score[i] });
            for (int i = 0; i < GameManager.GetInstance().currency.Count; i++) OnPlayerCurrencyChanged(new NetworkListEvent<int>() { Index = i, Value = GameManager.GetInstance().currency[i] });
            for (int i = 0; i < DefensePointManager.GetInstance().health.Count; i++) OnDefenseHealthChanged(new NetworkListEvent<int>() { Index = i, Value = DefensePointManager.GetInstance().health[i] });

            //get towers of the player selected class and instantiate them as buttons
            List<ClassTower> towers = GameData.GetInstance().GetMyClass().towers;
            for(int i = 0; i < towers.Count; i++)
            {
                GameObject buttonObj = Instantiate(towerButton);
                UIBuyButton buyButton = buttonObj.GetComponent<UIBuyButton>();
                buyButton.Initialize(towers[i]);

                int buttonIndex = i;
                buyButton.button.onClick.AddListener(() => SelectTower(buttonIndex));
                buttonObj.transform.SetParent(buyActions.transform, false);
                buttonObj.transform.SetSiblingIndex(i);
            }

            //fill up the remaining button slots that are empty
            for (int i = towers.Count; i < 5; i++)
            {
                GameObject buttonObj = Instantiate(emptyButton);
                buttonObj.transform.SetParent(buyActions.transform, false);
                buttonObj.transform.SetSiblingIndex(i);
            }

            //play background music
            AudioManager.PlayMusic(1);
        }


        /// <summary>
        /// Called when a tower buy button was clicked.
        /// Enables placement mode and shows the floating tower.
        /// </summary>
        public void SelectTower(int index)
        {
            Destroy(towerFloating);

            ClassTower classTower = GameData.GetInstance().GetMyClassTower(index);

            towerFloating = Instantiate(TowerManager.GetInstance().placeFX, Vector3.zero, Quaternion.identity);
            towerFloating.GetComponent<TowerGhost>().Initialize(classTower.data);
            towerFloating.SetActive(false);
            towerIndex = index;
            buyTooltip.Initialize(classTower);

            ToggleTowerPlacement(true);
        }


        /// <summary>
        /// Toggles visibility of all available grids when entering or leaving placement mode.
        /// </summary>
        public void ToggleTowerPlacement(bool active)
        {
            GridManager.GetInstance().ToggleGridPlacement((short)GameManager.GetInstance().localPlayer.thisColor, active);
            buyActions.transform.GetChild(buyActions.transform.childCount - 1).GetComponent<Button>().interactable = active;
            selectMode = active ? SelectMode.Grid : SelectMode.Tower;
        }


        //checks for input in Grid or Tower selection mode
        //does the raycasts for actually detecting what was hit
        void Update()
        {
            //input over UI, do not do any raycasts
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            switch (selectMode)
            {
                //selecting grid for placing a new tower
                case SelectMode.Grid:

                    #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
                    inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (RaycastGrid())
                    {
                        if (Input.GetMouseButtonDown(0)) AddTower();
                    }
                    else
                    {
                        towerFloating.SetActive(false);
                    }
                    #else
                    if (Input.touchCount > 0)
                    {
                        switch (Input.GetTouch(0).phase)
                        {
                            case TouchPhase.Began:
                            case TouchPhase.Moved:
                                inputRay = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
                                if (!RaycastGrid()) towerFloating.SetActive(false);
                                break;

                            case TouchPhase.Ended:
                                if (RaycastGrid()) AddTower();
                                break;
                        }
                    }
                    #endif
                    break;

                //selecting tower for upgrading or selling it
                case SelectMode.Tower:

                    #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
                    if (Input.GetMouseButtonDown(0))
                    {
                        inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                        if (RaycastTower()) ShowTowerUpgrade();
                    }
                    #else
                    if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                    {
                        inputRay = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
                        if (RaycastTower()) ShowTowerUpgrade();
                    }
                    #endif
                    break;
            }
        }


        //raycast logic against game objects on the Grid layer
        bool RaycastGrid()
        {
            if (Physics.Raycast(inputRay, out inputHit, float.MaxValue, GridManager.layerMask))
            {
                //draw line for visualization in the editor
                Debug.DrawLine(inputRay.origin, inputHit.point);

                //get grid transform that was hit
                //afterwards we convert hit point into local space for that transform
                Transform trans = inputHit.transform;
                Vector3 localHit = trans.InverseTransformPoint(inputHit.point);
                GridPlacement grid = trans.GetComponent<GridPlacement>();

                //even though only the game object tiles for this player have been activated,
                //raycast still works on all GridPlacement components so we have to check for the client ID again
                if (grid.clientID >= 0 && grid.clientID != (short)GameManager.GetInstance().localPlayer.thisColor)
                    return false;

                //from the local hit position we can get the exact tile that was hit in this grid
                hitTile = grid.GetTileAtPosition(localHit);

                //if the tower could be placed here, move the floating tower there
                if (hitTile != null && !hitTile.IsOccupied())
                {
                    towerFloating.transform.position = hitTile.transform.position;
                    towerFloating.transform.rotation = grid.transform.rotation;
                    towerFloating.SetActive(true);

                    return true;
                }
            }

            //nothing was hit or invalid
            return false;
        }


        //raycast logic against game objects on the Tower layer
        bool RaycastTower()
        {
            if (Physics.Raycast(inputRay, out inputHit, float.MaxValue, TowerManager.layerMask))
            {
                //draw line for visualization in the editor
                Debug.DrawLine(inputRay.origin, inputHit.point);

                return true;
            }

            //nothing was hit or invalid
            return false;
        }


        //send a request to the server to place the tower on the location that was clicked
        void AddTower()
        {
            TowerManager.GetInstance().AddTowerServerRpc((short)towerIndex, inputHit.point);
            CancelAction();
        }


        //show the upgrade actions including upgrade tooltip for the selected tower
        void ShowTowerUpgrade()
        {
            Transform trans = inputHit.transform;
            selectedTower = trans.GetComponent<Tower>();
            buyActions.SetActive(false);

            upgradeActions.SetActive(true);
            upgradeActions.GetComponentInChildren<UIUpgradeButton>().Initialize(selectedTower);
            upgradeActions.GetComponentInChildren<UISellButton>().Initialize(selectedTower);

            upgradeTooltip.Initialize(selectedTower);
        }


        /// <summary>
        /// Sends a request to the server to upgrade the tower that was selected using its network ID.
        /// </summary>
        public void UpgradeTowerAction()
        {
            TowerManager.GetInstance().UpgradeTowerServerRpc(selectedTower.NetworkObjectId);
            upgradeTooltip.gameObject.SetActive(false);            
        }


        /// <summary>
        /// Sends a request to the server to sell the tower that was selected using its network ID.
        /// </summary>
        public void SellTowerAction()
        {
            TowerManager.GetInstance().SellTowerServerRpc(selectedTower.NetworkObjectId);
        }


        /// <summary>
        /// Cancels existing selections or leaves placement mode, if it was active beforehand.
        /// </summary>
        public void CancelAction()
        {
            //destroy floating tower in placement mode
            if (selectMode == SelectMode.Grid)
            {
                Destroy(towerFloating);
                buyTooltip.gameObject.SetActive(false);
                ToggleTowerPlacement(false);
            }

            selectedTower = null;

            //disable upgrade actions and return to initial state
            upgradeActions.SetActive(false);
            upgradeTooltip.gameObject.SetActive(false);
            buyActions.SetActive(true);
        }


        /// <summary>
        /// Hooked into the WaveManager wavePreEvent displaying time delay to the next wave.
        /// </summary>
        public void OnWavePreEvent(int delay)
        {
            StartCoroutine(WaveTextRoutine(delay));
        }


        /// <summary>
        /// Reads the players currently connected and sets their names in the Stats window.
        /// </summary>
        public void OnPlayerNameChanged()
        {
            foreach (PlayerStruct playerStruct in NetworkManagerCustom.GetInstance().playerData)
            {
                playerNames[playerStruct.thisColor].text = playerStruct.thisName.ToString();
            }
        }


        /// <summary>
        /// Method called by the NetworkList operation over the Network when its content changes.
        /// This is an implementation for changes to the player currency, updating the text values.
        /// Parameters: event for the currency entry which received updates.
        /// </summary>
        public void OnPlayerCurrencyChanged(NetworkListEvent<int> changeEvent)
        {
            playerCurrency[changeEvent.Index].text = changeEvent.Value.ToString();

            if(changeEvent.Index == GameManager.GetInstance().localPlayer.thisColor)
            {
                localCurrency.text = changeEvent.Value.ToString();
            }
        }


        /// <summary>
        /// Method called by the NetworkList operation over the Network when its content changes.
        /// This is an implementation for changes to the player score, updating the text values.
        /// Parameters: event for the score entry which received updates.
        /// </summary>
        public void OnPlayerScoreChanged(NetworkListEvent<int> changeEvent)
        {
            playerScore[changeEvent.Index].text = changeEvent.Value.ToString();

            if (changeEvent.Index == GameManager.GetInstance().localPlayer.thisColor)
            {
                localScore.text = changeEvent.Value.ToString();
            }
        }


        /// <summary>
        /// Method called by the NetworkList operation over the Network when its content changes.
        /// This is an implementation for changes to the defense point health, updating the text values.
        /// Parameters: event for the DefensePoint entry which received updates.
        /// </summary>
        public void OnDefenseHealthChanged(NetworkListEvent<int> changeEvent)
        {
            //set the value in the Stats window
            defenseHealth[changeEvent.Index].text = changeEvent.Value.ToString();

            //find the lowest health
            float lowest = 1f;
            float current;
            foreach (int health in DefensePointManager.GetInstance().health)
            {
                current = (float)health / DefensePointManager.GetInstance().startHealth;
                if (current < lowest) lowest = current;
            }

            //set the health summary as a percentage to that lowest value
            defenseSummary.text = (lowest * 100) + "%";
        }


        /// <summary>
        /// Hooked into the TowerManager changeTowerEvent
        /// to deselect the current tower if that's the one that was changed.
        /// </summary>
        public void OnChangeTowerEvent(ulong networkId)
        {
            if (selectedTower == null || selectedTower.NetworkObjectId == networkId)
            {
                CancelAction();
            }
        }


        //coroutine updating the waveText with remaining delay over time
        IEnumerator WaveTextRoutine(int delay)
        {
            int timeLeft = delay;

            while(timeLeft > 0)
            {
                waveText.text = "WAVE " + WaveManager.waveIndex + ": " + timeLeft;
                timeLeft--;
                yield return new WaitForSeconds(1);
            }

            waveText.text = "WAVE " + WaveManager.waveIndex;
        }


        /// <summary>
        /// Set game end text and display game result.
        /// </summary>
        public void SetGameOverText(bool success)
        {
            //show game result and colorize it using UI markup
            Color color = success ? Color.green : Color.red;
            string text = success ? "SUCCESS!" : "DEFEAT!";

            gameOverText.text = "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + text + "</color>";
            gameOverText.gameObject.SetActive(true);
        }


        /// <summary>
        /// Displays the game's end screen. Called by GameManager.
        /// </summary>
        public void ShowGameOver()
        {
            gameOverWindow.SetActive(true);
        }


        /// <summary>
        /// Stops receiving further network updates by hard disconnecting, then loading starting scene.
        /// </summary>
        public void Disconnect()
        {
            NetworkManager.Singleton.Shutdown();
            Time.timeScale = 1;

            SceneManager.LoadScene(0);
        }


        //unsubscribe callbacks
        void OnDestroy()
        {
            WaveManager.wavePreEvent -= OnWavePreEvent;
            TowerManager.changeTowerEvent -= OnChangeTowerEvent;
        }
    }


    /// <summary>
    /// Selection mode that is currently active and performing input raycasts.
    /// </summary>
    public enum SelectMode
    {
        /// <summary>
        /// Input checks against Grids when trying to place a new tower.
        /// </summary>
        Grid,

        /// <summary>
        /// Input checks against Towers when not in placement mode.
        /// </summary>
        Tower
    }
}