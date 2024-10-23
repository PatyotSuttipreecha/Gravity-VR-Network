/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseMP
{
    /// <summary>
    /// Data container that describes the definition and content of a class.
    /// Each class can have a unique set of towers available for selection in-game.
    /// </summary>
    [CreateAssetMenu(fileName = "ClassData", menuName = "ScriptableObjects/ClassData", order = 2)]
    public class ClassDataScriptableObject : ScriptableObject
    {
        /// <summary>
        /// Name of the class displayed in the Lobby selection.
        /// </summary>
        public string className;

        /// <summary>
        /// The product identifier if matched with an App Store product.
        /// </summary>
        public string productId;

        /// <summary>
        /// Whether this product should be available on start or purchased later.
        /// </summary>
        public bool owned = true;

        /// <summary>
        /// List of towers available for selection in the class.
        /// </summary>
        public List<ClassTower> towers;
    }


    /// <summary>
    /// Definition of a tower in the UI and for placement during the game.
    /// </summary>
    [Serializable]
    public class ClassTower
    {
        /// <summary>
        /// The name of the tower shown in tooltips.
        /// </summary>
        public string name;

        /// <summary>
        /// Prefab instantiated when placing the tower.
        /// </summary>
        public GameObject prefab;

        /// <summary>
        /// Image displayed on a UIBuyButton.
        /// </summary>
        public Sprite icon;

        /// <summary>
        /// Reference to tower data used during placement and in tooltips.
        /// </summary>
        [HideInInspector]
        public TowerDataScriptableObject data;
    }
}