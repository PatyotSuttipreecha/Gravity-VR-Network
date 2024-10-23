/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

namespace TowerDefenseMP
{
    /// <summary>
    /// Button component to upgrade a tower.
    /// </summary>
    public class UIUpgradeButton : MonoBehaviour
    {
        /// <summary>
		/// Reference for the price the tower is upgraded for.
		/// </summary>
        public Text price;

        /// <summary>
		/// Button reference this script is assigned to.
		/// </summary>
        [HideInInspector]
        public Button button;

        private int priceAmount;


        //get references
        void Awake()
        {
            button = GetComponent<Button>();
        }


        /// <summary>
		/// Initializes this script with the tower passed in.
		/// </summary>
        public void Initialize(Tower selectedTower)
        {
            //this is called on every new selection,
            //so we do not need an OnEnable method in this class

            //there is no next tower we could upgrade to
            if (selectedTower.data.nextPrefab == null)
            {
                priceAmount = int.MaxValue;
                price.text = string.Empty;
                button.interactable = false;
                return;
            }

            //subscribe to currency changes so we can update the button live
            GameManager.GetInstance().currency.OnListChanged += OnPlayerCurrencyChanged;

            //get and set price text
            priceAmount = selectedTower.data.nextData.price;
            price.text = priceAmount.ToString();

            UpdatePurchasable();
        }


        //invoke the currency event for our player to update the button interactable state
        void UpdatePurchasable()
        {
            int localClientId = (int)NetworkManager.Singleton.LocalClientId;
            OnPlayerCurrencyChanged(new NetworkListEvent<int>() { Index = localClientId, Value = GameManager.GetInstance().currency[localClientId] });
        }


        //method called when currency amount changes
        void OnPlayerCurrencyChanged(NetworkListEvent<int> changeEvent)
        {
            //do not execute further changes if the currency entry is not ours
            int localClientId = (int)NetworkManager.Singleton.LocalClientId;
            if (changeEvent.Index != localClientId)
                return;

            //make the button interactable or not based on currently owned currency amount
            button.interactable = changeEvent.Value >= priceAmount;
        }


        //unsubscribe from currency events again
        void OnDisable()
        {
            GameManager.GetInstance().currency.OnListChanged -= OnPlayerCurrencyChanged;
        }
    }
}
