/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using UnityEngine;

namespace TowerDefenseMP
{
    /// <summary>
    /// Orientates the gameobject this script is attached to to always face the camera.
    /// </summary>
    public class UIBillboard : MonoBehaviour
    {
        /// <summary>
        /// If enabled, scales this object to always stay at the same size,
        /// regardless of the position in the scene i.e. distance to camera.
        /// </summary>
        public bool scaleWithDistance = false;

        /// <summary>
        /// Multiplier applied to the distance scale calculation.
        /// </summary>
        public float scaleMultiplier = 1f;

        //cache reference to camera transform
        private Transform camTrans;
        
        //cache reference to this transform
        private Transform trans;

        //calculated size depending on camera distance
        private float size;


        //get references
        void Awake()
        {
            camTrans = Camera.main.transform;
            trans = transform;
        }


        //always face the camera every frame
        void LateUpdate()
        {
            transform.LookAt(trans.position + camTrans.rotation * Vector3.forward, camTrans.rotation * Vector3.up);

            if (!scaleWithDistance) return;
            size = (camTrans.position - transform.position).magnitude;
            transform.localScale = Vector3.one * (size * (scaleMultiplier / 100f));
        }
    }
}
