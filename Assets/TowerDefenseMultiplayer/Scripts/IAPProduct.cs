/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using UnityEngine.UI;
#if UNITY_IAP
using UnityEngine.Purchasing;
#endif

namespace TowerDefenseMP
{
    /// <summary>
    /// Describes an in-app purchase product that can be bought by using Unity IAP.
    /// Contains several UI elements and logic for selecting/deselecting.
    /// </summary>
    public class IAPProduct : MonoBehaviour
    {
        /// <summary>
        /// The unique identifier for this product.
        /// For live products, this should match the id on the App Store.
        /// </summary>
        public string id;

        #if UNITY_IAP
        /// <summary>
        /// In-app purchase type that should match the product type on the App Store.
        /// </summary>
        public ProductType type = ProductType.NonConsumable;
        #endif

        /// <summary>
        /// UI button that triggers the purchase workflow via Unity IAP.
        /// </summary>
        public GameObject buyButton;

        /// <summary>
        /// Optional elements which get enabled if this product has been sold.
        /// </summary>
        public GameObject sold;


        //sets the initial purchase/selection state
        void Awake()
        {
            UnityIAPManager.purchaseSuccessEvent += Initialize;

            #if UNITY_IAP
            if (UnityIAPManager.controller != null)
                Initialize();
            #endif
        }


        private void Initialize()
        {
            #if UNITY_IAP
            Product p = UnityIAPManager.controller.products.WithID(id);
            if(p != null) Purchased(p.hasReceipt);
            #endif
        }


        private void Initialize(string productId)
        {
            if (productId == id) Initialize();
        }


        /// <summary>
        /// Tries to open the purchase dialog this product via Unity IAP.
        /// </summary>
        public void Purchase()
        {
            #if UNITY_IAP
            UnityIAPManager.PurchaseProduct(id);
            #endif
        }


        /// <summary>
        /// Sets this product UI state to 'purchased', hiding the buy button
        /// and showing the 'sold' gameobject, if specified.
        /// </summary>
        public void Purchased(bool state)
        {
            buyButton.SetActive(!state);
            if (sold) sold.SetActive(state);
        }


        private void OnDestroy()
        {
            UnityIAPManager.purchaseSuccessEvent -= Initialize;
        }
    }
}
