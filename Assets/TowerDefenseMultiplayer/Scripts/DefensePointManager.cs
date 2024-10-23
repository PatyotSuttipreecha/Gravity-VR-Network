/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace TowerDefenseMP
{
    /// <summary>
    /// Handling all networked player damage with a list of health entries.
    /// This is a server-authoritative component so only the server should initialize and modify it.
    /// </summary>
    public class DefensePointManager : NetworkBehaviour
    {
        //reference to this script instance
        private static DefensePointManager instance;

        /// <summary>
		/// Mask for all DefensePoint game objects, if necessary for e.g. raycasting against them.
        /// </summary>
        public static int layerMask;

        /// <summary>
        /// Whether all DefensePoint components on children should be added automatically.
        /// </summary>
        public bool autoAddChildren;

        /// <summary>
        /// Networked list storing health for each defense point.
        /// </summary>
        public NetworkList<int> health;

        /// <summary>
        /// The start health that is applies to all entries in the health list.
        /// </summary>
        public int startHealth = 50;

        /// <summary>
        /// A list of all DefensePoint components placed in the scene.
        /// </summary>
        public List<DefensePoint> defensePoints;


        //initialize variables
        void Awake()
        {
            instance = this;
            layerMask = LayerMask.GetMask("Defense");

            health = new NetworkList<int>();

            if (autoAddChildren)
            {
                for(int i = 0; i < transform.childCount; i++)
                {
                    DefensePoint def = transform.GetChild(i).GetComponent<DefensePoint>();

                    if (!defensePoints.Contains(def))
                        defensePoints.Add(def);
                }
            }
        }


        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static DefensePointManager GetInstance()
        {
            return instance;
        }


        /// <summary>
        /// After successful network start, initialize variables
        /// </summary>
        public override void OnNetworkSpawn()
        {
            //server only
            if (!IsServer)
                return;

            //add start health to all health entries
            for (int i = 0; i < defensePoints.Count; i++)
            {
                health.Add(startHealth);
            }
        }


        /// <summary>
        /// Server only: apply damage taken by the defense point.
        /// Sends a game over RPC to other clients if health is zero.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void TakeDamageServerRpc(ulong networkID)
        {
            for (int i = 0; i < defensePoints.Count; i++)
            {
                if (defensePoints[i].NetworkObjectId == networkID)
                {
                    if (health[i] > 0) health[i]--;
                    if (health[i] == 0) GameManager.GetInstance().GameOverClientRpc(false);
                    break;
                }
            }
        }
    }
}