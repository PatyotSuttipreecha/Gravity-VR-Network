/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using UnityEngine;

namespace TowerDefenseMP
{
    /// <summary>
    /// The data container that is assigned on a Tower prefab and component.
    /// Allows cross-referencing or using it on multiple towers but with editing only once.
    /// </summary>
    [CreateAssetMenu(fileName = "TowerData", menuName = "ScriptableObjects/TowerData", order = 3)]
    public class TowerDataScriptableObject : ScriptableObject
    {
        /// <summary>
        /// The initial or upgrade cost.
        /// </summary>
        public int price;

        /// <summary>
        /// The damage dealt to Unit per shot.
        /// </summary>
        public int damage;

        /// <summary>
        /// Radius for the tower to detect Unit within.
        /// </summary>
        public int range;

        /// <summary>
        /// Time delayed between shots.
        /// </summary>
        public float delay;

        /// <summary>
        /// The projectile to spawn at each shot.
        /// </summary>
        public GameObject projectile;

        /// <summary>
        /// Type of the assigned projectile prefab.
        /// </summary>
        public ProjectileType projectileType;

        /// <summary>
        /// The next tower prefab this data can upgrade to.
        /// If null, there is no upgrade.
        /// </summary>
        public GameObject nextPrefab;

        /// <summary>
        /// The data container of next tower's upgrade.
        /// This will be assigned by GameData during initialization.
        /// </summary>
        [HideInInspector]
        public TowerDataScriptableObject nextData;
    }
}
