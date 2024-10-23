/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using System.Collections.Generic;
using UnityEngine;

namespace TowerDefenseMP
{
    /// <summary>
    /// Manipulates shared Material on a LineRenderer to get the impression of an animation.
    /// </summary>
    public class LineRendererUV : MonoBehaviour
    {
        /// <summary>
        /// List of Pool components that contain a prefab spawned with a LineRenderer.
        /// </summary>
        public List<Pool> pools = new List<Pool>();

        /// <summary>
        /// Multiplier value applied to the update speed.
        /// </summary>
        public int multiplier = 10;

        private List<Material> list = new List<Material>();
        private Vector2 offset;


        //find LineRenderer references in assigned pools and get their material
        void Start()
        {
            for(int i = 0; i < pools.Count; i++)
            {
                list.Add(pools[i].inactive[0].GetComponent<LineRenderer>().sharedMaterial);
            }
        }


        //set texture offset of shared materials over time
        void Update()
        {
            for(int i = 0; i < list.Count; i++)
            {
                list[i].SetTextureOffset("_MainTex", offset);

                offset.x -= Time.deltaTime * multiplier;
                if (offset.x < -10) offset.x = 0;
            }
        }
    }
}
