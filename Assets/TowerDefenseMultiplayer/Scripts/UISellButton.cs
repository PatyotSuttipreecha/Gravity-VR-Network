/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using UnityEngine.UI;

namespace TowerDefenseMP
{
    /// <summary>
    /// Button component to sell a tower.
    /// </summary>
    public class UISellButton : MonoBehaviour
    {
        /// <summary>
		/// Reference for the price the tower is sold for.
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

            //we can only sell our own towers
            if (!selectedTower.NetworkObject.IsOwner)
            {
                price.text = string.Empty;
                button.interactable = false;
                return;
            }

            //get and set price text
            priceAmount = TowerManager.GetInstance().GetSellAmount(selectedTower);
            price.text = priceAmount.ToString();
            button.interactable = true;
        }
    }
}
