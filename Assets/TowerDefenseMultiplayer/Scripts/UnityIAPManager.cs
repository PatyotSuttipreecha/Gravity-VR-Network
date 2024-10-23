/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

#if UNITY_IAP
using Unity.Services.Core;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif
#if RECEIPT_VALIDATION
using FLOBUK.ReceiptValidator;
#endif

using System;
using UnityEngine;

namespace TowerDefenseMP
{
    /// <summary>
    /// Manager handling the full in-app purchase workflow,
    /// granting purchases and catching errors using Unity IAP.
    /// </summary>
    #if UNITY_IAP
    public class UnityIAPManager : MonoBehaviour, IDetailedStoreListener
    #else
    public class UnityIAPManager : MonoBehaviour
    #endif
    {
        #pragma warning disable 0067
        /// <summary>
        /// Fired after successful billing initialization.
        /// </summary>
        public static event Action initializedEvent;

        /// <summary>
        /// Fired on failed purchase to deliver its product identifier.
        /// </summary>
        public static event Action<string> purchaseFailedEvent;

        /// <summary>
        /// Fired on successful purchase to deliver its product identifier.
        /// </summary>
        public static event Action<string> purchaseSuccessEvent;
        #pragma warning restore 0067

        #if UNITY_IAP
        //disable platform specific warnings, because Unity throws them
        //for unused variables however they are used in this context
        #pragma warning disable 0414
        /// <summary>
        /// Reference to the Unity IAP controller for accessing retrieved product data.
        /// </summary>
        public static IStoreController controller;

        private static ConfigurationBuilder builder;
        private static IExtensionProvider extensions;
        #pragma warning restore 0414


        //start initialization process
        void Start()
        {
            //construct IAP purchasing instance
            builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            //iterate over all IAPProducts found in the scene and add their id to be looked up by Unity IAP
            IAPProduct[] products = Resources.FindObjectsOfTypeAll<IAPProduct>();
            foreach(IAPProduct product in products)
            {
				builder.AddProduct(product.id, product.type);
            }

            Initialize();
        }


        /// <summary>
        /// Initialize core services and Unity IAP.
        /// </summary>
        public async void Initialize()
        {
            try
            {
                //Unity Gaming Services are required first
                await UnityServices.InitializeAsync();

                //now we're ready to initialize Unity IAP
                UnityPurchasing.Initialize(this, builder);
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message);
                OnInitializeFailed(InitializationFailureReason.PurchasingUnavailable);
            }
        }


        /// <summary>
        /// Called when Unity IAP is ready to make purchases, delivering the store controller
        /// (contains all online products) and platform specific extension
        /// </summary>
        public void OnInitialized (IStoreController ctrl, IExtensionProvider ext)
        {
            //cache references
            controller = ctrl;
            extensions = ext;
            initializedEvent?.Invoke();

            //restore currently owned products
            foreach (Product product in controller.products.all)
            {
                if (product.hasReceipt) OnPurchaseSuccess(product);
            }

            //server receipt validation addon
            #if RECEIPT_VALIDATION
                ReceiptValidator.Instance.Initialize(controller, builder);
                ReceiptValidator.purchaseCallback += OnPurchaseResult;
            #endif
        }


        /// <summary>
        /// Called when the user presses the 'Buy' button on an IAPProduct.
        /// </summary>
        public static void PurchaseProduct(string productId)
        {
            #if UNITY_EDITOR
            Debug.LogWarning("IAP Purchases are disabled in the Unity Editor since they would make persistent changes to ScriptableObjects. Please test in a build / real device!");
            #else
            if(controller != null)
               controller.InitiatePurchase(productId);
            #endif
        }


        /// <summary>
        /// Called when Unity IAP encounters an unrecoverable initialization error.
        /// </summary>
        public void OnInitializeFailed (InitializationFailureReason error)
        {
			Debug.Log(error);
        }


        /// <summary>
        /// Overload for the failed initialization callback delivering additional error message.
        /// </summary>
        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            OnInitializeFailed(error);
        }


        /// <summary>
        /// Called when a purchase completes after being bought.
        /// </summary>
        public PurchaseProcessingResult ProcessPurchase (PurchaseEventArgs e)
        {
            Product product = e.purchasedProduct;

            //server receipt validation addon
            #if RECEIPT_VALIDATION
                PurchaseState state = ReceiptValidator.Instance.RequestPurchase(product);
                //handle what happens with the product next
                switch (state)
                {
                    case PurchaseState.Pending:
                        return PurchaseProcessingResult.Pending;
                    case PurchaseState.Failed:
                        product = null;
                        break;
                }                
            #endif

            //with the transaction finished, just call our purchase method
            if(product != null)
            { 
                OnPurchaseSuccess(product);
            }

            //return that we are done with processing the transaction
            return PurchaseProcessingResult.Complete;
        }


        #if RECEIPT_VALIDATION
        //this is called with the result from the server receipt validation request
        void OnPurchaseResult(bool success, SimpleJSON.JSONNode data)
        {
            if (!success) return;

            Product product = controller.products.WithID(data["data"]["productId"]);
            OnPurchaseSuccess(product);
        }
        #endif


        //the product passed in was purchased successfully
        //here we loop over our classes and set the matching one to purchased
        void OnPurchaseSuccess(Product purchasedProduct)
        {
            foreach(ClassDataScriptableObject classData in GameData.GetInstance().classes)
            {
                if(purchasedProduct.definition.id == classData.productId)
                {
                    classData.owned = true;
                    break;
                }
            }

            purchaseSuccessEvent?.Invoke(purchasedProduct.definition.id);
        }


        /// <summary>
        /// Called when a purchase fails, providing the product and reason.
        /// </summary>
        public void OnPurchaseFailed(Product p, PurchaseFailureReason r)
        {
            purchaseFailedEvent?.Invoke(r.ToString());
        }


        /// <summary>
        /// Overload for the failed purchase callback, delivering additional description.
        /// </summary>
        public void OnPurchaseFailed(Product p, PurchaseFailureDescription d)
        {
            OnPurchaseFailed(p, d.reason);
        }


        /// <summary>
        /// Method for restoring transactions (prompts for password on iOS).
        /// </summary>
        public static void RestoreTransactions()
        {
            #if UNITY_IOS
            if(extensions != null)
			    extensions.GetExtension<IAppleExtensions>().RestoreTransactions(null);
            #elif RECEIPT_VALIDATION
                ReceiptValidator.Instance.RequestRestore();
            #endif
        }
        #endif
    }
}