/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using Unity.Netcode;

namespace TowerDefenseMP
{
    /// <summary>
    /// Component on a location in the map that needs to be defended from Unit.
    /// If a Unit reaches this location, the corresponding health entry takes damage.
    /// </summary>
    public class DefensePoint : NetworkBehaviour
    {
        /// <summary>
        /// Clip to play when this base gets hit.
        /// </summary>
        public AudioClip hitClip;

        /// <summary>
        /// Object to spawn when this base gets hit.
        /// </summary>
        public GameObject hitFX;


        /// <summary>
        /// Called by Unit when reaching the end of its path. The server then applies damage
        /// to the matching health entry which gets synced to other clients in a later frame.
        /// </summary>
        public void Hit()
        {
            //create clips and particles on hit
            if (hitFX) PoolManager.Spawn(hitFX, transform.position, Quaternion.identity);
            if (hitClip) AudioManager.Play3D(hitClip, transform.position);

            //the previous code is not synced to clients at all, because visual stuff does not need to be.
            //at this point, continue with the critical game aspects only on the server
            if (!IsServer) return;
            DefensePointManager.GetInstance().TakeDamageServerRpc(NetworkObjectId);
        }


        //draws some gizmos for better visualization in the editor
        #if UNITY_EDITOR
        void OnDrawGizmos()
        {
            UnityEditor.Handles.color = new Color(1, 0, 0, 0.1f);
            UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.up, 2);
        }
        #endif
    }
}