/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseMP
{
    /// <summary>
    /// Managing all Grid actions and providing methods for convenience.
    /// Such as checking click position on a grid or toggling all of them.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        //reference to this script instance
        private static GridManager instance;

        /// <summary>
		/// Mask for all Grid game objects, if necessary for e.g. raycasting against them.
        /// </summary>
        public static int layerMask;

        /// <summary>
        /// Whether all GridPlacement components on children should be added automatically.
        /// </summary>
        public bool autoAddChildren;

        /// <summary>
        /// A list of all GridPlacement components placed in the scene.
        /// </summary>
        public List<GridPlacement> grids;


        //initialize variables
        void Awake()
        {
            instance = this;
            layerMask = LayerMask.GetMask("Grid");

            if (autoAddChildren)
            {
                GridPlacement[] childs = GetComponentsInChildren<GridPlacement>();
                for (int i = 0; i < childs.Length; i++)
                {
                    if (!grids.Contains(childs[i]))
                        grids.Add(childs[i]);
                }
            }
        }


        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static GridManager GetInstance()
        {
            return instance;
        }


        /// <summary>
        /// Returns the component of a grid based on a position in local space passed in.
        /// This is a convenience method used in other manager components.
        /// </summary>
        public GridPlacement GetGridAtPosition(Vector3 hitPoint)
        {
            for(int i = 0; i < grids.Count; i++)
            {
                if (grids[i].IsPositionOnGrid(hitPoint))
                    return grids[i];
            }

            return null;
        }


        /// <summary>
        /// Toggles visibility of all grids where the player is allowed to place towers.
        /// </summary>
        public void ToggleGridPlacement(short clientID, bool state)
        {
            for(int i = 0; i < grids.Count; i++)
            {
                if (grids[i].clientID >= 0 && grids[i].clientID != clientID)
                    continue;

                grids[i].ToggleVisibility(state);
            }
        }
    }
}