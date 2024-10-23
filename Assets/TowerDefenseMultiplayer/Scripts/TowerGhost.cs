/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using UnityEngine;

namespace TowerDefenseMP
{
    /// <summary>
    /// This component is added to the floating tower positioned on a grid for visualization purposes,
    /// before the tower is actually placed or bought. It allows pre-viewing until switched out with the real tower.
    /// </summary>
    public class TowerGhost : MonoBehaviour
    {
        /// <summary>
        /// A 3d visualization of the range the tower will be able to shoot targets within.
        /// </summary>
        public Transform rangeIndicator;


        /// <summary>
		/// Initializes this script with the tower passed in.
        /// </summary>
        public void Initialize(TowerDataScriptableObject data)
        {
            rangeIndicator.localScale = new Vector3(data.range * 2, data.range * 2, 1);
        }
    }
}
