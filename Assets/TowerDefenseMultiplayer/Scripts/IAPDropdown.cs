/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if UNITY_IAP
using UnityEngine.Purchasing;
#endif

namespace TowerDefenseMP
{
    /// <summary>
    /// Attached on a UILobbyPlayer prefab, on its class selection dropdown.
    /// This makes sure that only bought classes are available for selection in the dropdown.
    /// </summary>
    public class IAPDropdown : MonoBehaviour, IPointerClickHandler
    {
        /// <summary>
        /// If the dropdown is clicked, loops over all options and makes them
        /// interactable based on whether the corresponding class product is owned or not.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            Transform listParent = eventData.selectedObject.transform.parent;

            for (int i = 1; i < listParent.childCount; i++)
            {
                Toggle toggle = listParent.GetChild(i).GetComponent<Toggle>();
                if(toggle != null) toggle.interactable = GameData.GetInstance().classes[i - 1].owned;
            } 
        }
    }
}
