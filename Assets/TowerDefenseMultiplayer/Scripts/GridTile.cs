/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using UnityEngine;

namespace TowerDefenseMP
{
    /// <summary>
    /// Defines a single tile on a grid where towers can be placed onto.
    /// Is set to occupied or freed to visualize whether a tower has been placed.
    /// </summary>
    public class GridTile : MonoBehaviour
    {
        /// <summary>
        /// Starting color when the tile is not occupied.
        /// </summary>
        public Color baseColor = Color.white;

        /// <summary>
        /// Reference to the renderer of the tile.
        /// </summary>
        public Renderer tileRenderer;

        /// <summary>
        /// Reference to the renderer of the tile's minimap icon.
        /// </summary>
        public Renderer iconRenderer;


        //initialize variables
        void Awake()
        {
            ClearOccupied();    
        }


        /// <summary>
        /// Checks the tile color to find out whether it is currently occupied.
        /// </summary>
        public bool IsOccupied()
        {
            return tileRenderer.material.color != baseColor;
        }


        /// <summary>
        /// Applies the player's color to the tile and minimap icon when a tower has been placed.
        /// </summary>
        public void SetOccupied(short colorIndex)
        {
            Color targetColor;
            if (ColorUtility.TryParseHtmlString(((ClassColor)colorIndex).ToString(), out targetColor))
            {
                tileRenderer.material.color = targetColor;
                iconRenderer.material.color = targetColor;
                iconRenderer.gameObject.SetActive(true);
                gameObject.SetActive(true);
            }
        }


        /// <summary>
        /// Sets all renderers back to the starting color.
        /// </summary>
        public void ClearOccupied()
        {
            gameObject.SetActive(false);
            iconRenderer.gameObject.SetActive(false);
            tileRenderer.material.color = baseColor;
            iconRenderer.material.color = baseColor;
        }
    }
}