/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using UnityEngine;

namespace TowerDefenseMP
{
    /// <summary>
    /// Inherits from Projectile making use of a LineRenderer for simulation.
    /// </summary>
    public class ProjectileLine : Projectile
    {
        /// <summary>
        /// Delay how often damage should be applied to target Unit.
        /// </summary>
        public float fireInterval = 0.2f;

        private LineRenderer line;
        private float nextFire;


        /// <summary>
        /// Use base implementation.
        /// Additionally set LineRenderer reference.
        /// </summary>
        public override void Awake()
        {
            base.Awake();
            line = GetComponent<LineRenderer>();
        }


        /// <summary>
        /// Set LineRenderer positions (start, end) on spawn.
        /// </summary>
        public void OnSpawn()
        {
            line.SetPosition(0, thisTrans.position);
            line.SetPosition(1, thisTrans.position);
            nextFire = Time.fixedTime;
        }


        //update end position of LineRenderer to target
        void Update()
        {
            if (target == null)
            {
                //despawn if target is not valid anymore
                PoolManager.Despawn(gameObject);
                return;
            }

            line.SetPosition(1, targetTrans.position);
        }


        //if nextFire delay has passed, apply damage to Unit again
        void FixedUpdate()
        {
            if (Time.fixedTime > nextFire && target != null && targetObj.activeInHierarchy)
            {
                //target was damaged, spawn hit effect
                if (hitFX != null)
                    PoolManager.Spawn(hitFX, targetTrans.position, Quaternion.identity);

                //apply damage to target Unit and reset nextFire time
                target.TakeDamage(damage, ownerId);
                nextFire = Time.fixedTime + fireInterval;
            }
        }
    }
}
