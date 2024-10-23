/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using System;

namespace TowerDefenseMP
{
    /// <summary>
    /// Component that spawns individual tiles vertically/horizontally and adds them to an internal array.
    /// </summary>
    public class GridPlacement : MonoBehaviour
    {
        /// <summary>
        /// NetworkID of a player that is allowed to place towers on these tiles (e.g. 0 = Player 1).
        /// If this value is negative all players are allowed to place towers here.
        /// </summary>
        public short clientID;

        /// <summary>
        /// Tile prefab that is spawned during initialization.
        /// </summary>
        public GameObject prefab;

        /// <summary>
        /// The size of each individual tile in localScale.
        /// </summary>
        public float size;

        /// <summary>
        /// Count of tiles to instantiate on the X axis.
        /// </summary>
        public int lengthX;

        /// <summary>
        /// Count of tiles to instantiate on the Z axis.
        /// </summary>
        public int lengthZ;

        /// <summary>
        /// Distance between spawned tiles.
        /// </summary>
        public float offset;

        private BoxCollider col;
        private GridTile[,] tiles;


        //initialize variables
        void Awake()
        {
            col = GetComponent<BoxCollider>();
            col.size = new Vector3(size * lengthX + (lengthX - 1) * offset, col.size.y, size * lengthZ + (lengthZ - 1) * offset);
            col.center = col.size / 2;
        }


        //spawn tiles
        void Start()
        {
            tiles = new GridTile[lengthX, lengthZ];
            Vector3[,] tilePositions = GetTilePositions();

            for (int i = 0; i < lengthX; i++)
            {
                for (int j = 0; j < lengthZ; j++)
                {
                    GridTile tile = Instantiate(prefab, transform, false).GetComponent<GridTile>();
                    tile.transform.localPosition = tilePositions[i, j];
                    tile.transform.localScale = new Vector3(size, 0, size);
                    tiles[i, j] = tile;
                }
            }
        }


        /// <summary>
        /// Toggles visibility of all tiles spawned by this component.
        /// Does not check for clientID like the GridManager.
        /// </summary>
        public void ToggleVisibility(bool state)
        {
            for (int i = 0; i < lengthX; i++)
            {
                for (int j = 0; j < lengthZ; j++)
                {
                    if (tiles[i, j].IsOccupied())
                        continue;

                    tiles[i, j].gameObject.SetActive(state);
                }
            }
        }


        /// <summary>
        /// Does a collider check whether a point is within its bounds.
        /// </summary>
        public bool IsPositionOnGrid(Vector3 hitPoint)
        {
            return col.ClosestPoint(hitPoint) == hitPoint;
        }


        /// <summary>
        /// Returns a spawned tile component based on a position in local space passed in.
        /// </summary>
        public GridTile GetTileAtPosition(Vector3 hitPoint)
        {
            //get tile coordinates as without offset
            int valueX = (int)(hitPoint.x / size);
            int valueZ = (int)(hitPoint.z / size);

            //apply offset to coordinates
            if (offset > 0)
            {
                valueX = GetTileIndexWithOffset(lengthX, hitPoint.x);
                valueZ = GetTileIndexWithOffset(lengthZ, hitPoint.z);

                if (valueX < 0 || valueZ < 0) return null;
            }

            //get tile from array
            GridTile tile = null;
            try
            {
               tile = tiles[valueX, valueZ];
            }
            catch (IndexOutOfRangeException) { }
            
            return tile;
        }


        //find coordinate with offset applied
        int GetTileIndexWithOffset(int length, float point)
        {
            float upper = 0;
            for (int i = 0; i < length; i++)
            {
                upper = (i + 1) * (size + offset);

                if (upper > point)
                {
                    float lower = upper - offset;
                    if (lower < point)
                    {
                        return -1;
                    }

                    return i;
                }
            }

            return -1;
        }

        //return a multi-dimensional array of tile locations where the grids are placed during initialization
        Vector3[,] GetTilePositions()
        {
            Vector3[,] tilesArray = new Vector3[lengthX, lengthZ];
            Vector3 initialPosition = new Vector3(size / 2 - 1, 0.05f, size / 2 - 1) - new Vector3(lengthX - 1, 0, lengthZ - 1);

            for (int i = 0; i < lengthX; i++)
            {
                for (int j = 0; j < lengthZ; j++)
                {
                    tilesArray[i, j] = initialPosition + new Vector3(lengthX + (i * size) + (i * offset), 0, lengthZ + (j * size) + (j * offset));
                }
            }

            return tilesArray;
        }


        //draws some gizmos in the editor,
        //but also not during play time
        void OnDrawGizmos()
        {
            if (Application.isPlaying)
                return;

            Gizmos.color = new Color(0, 1, 0, 0.1f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Vector3[,] tilePositions = GetTilePositions();

            for (int i = 0; i < lengthX; i++)
            {
                for(int j = 0; j < lengthZ; j++)
                {
                    Gizmos.DrawCube(tilePositions[i,j], new Vector3(size, 0.1f, size));
                }
            }
        }
    }
}