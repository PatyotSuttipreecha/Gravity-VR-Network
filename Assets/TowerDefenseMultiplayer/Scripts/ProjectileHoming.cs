/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using UnityEngine;

namespace TowerDefenseMP
{
    /// <summary>
    /// Inherits from Projectile for a homing missile like behavior.
    /// </summary>
    public class ProjectileHoming : Projectile
    {
        /// <summary>
        /// Speed multiplier applied to deltaTime.
        /// </summary>
        public float speed = 5f;
        
        //distance to target in this frame
        private float distanceFrame;


        //move projectile and try to hit target
        void Update()
        {
            if (target == null || !targetObj.activeInHierarchy)
            {
                //despawn if target is not valid anymore
                PoolManager.Despawn(gameObject);
                return;
            }

            //calculate direction to target
            Vector3 direction = targetTrans.position - thisTrans.position;

            //calculate distance moving in this frame
            //then compare whether target can be reached in this frame
            distanceFrame = speed * Time.deltaTime;
            if (direction.sqrMagnitude < (distanceFrame * distanceFrame))
            {
                //target was reached, spawn hit effect
                if (hitFX != null)
                    PoolManager.Spawn(hitFX, targetTrans.position, Quaternion.identity);
                
                //apply damage to target Unit and despawn projectile
                target.TakeDamage(damage, ownerId);
                PoolManager.Despawn(gameObject);
                return;
            }

            //otherwise, move further in direction by frame distance
            thisTrans.Translate(direction.normalized * distanceFrame, Space.World);
            thisTrans.LookAt(targetTrans);
        }
    }
}
