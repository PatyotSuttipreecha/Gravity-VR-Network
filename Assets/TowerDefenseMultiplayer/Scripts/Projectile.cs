/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using UnityEngine;

namespace TowerDefenseMP
{
    /// <summary>
    /// Projectile interface that can be inherited to create new projectile types.
    /// In its basic form, a projectile targets a single Unit and applies damage to it.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        /// <summary>
        /// FX that is instantiated when the target was hit.
        /// </summary>
        public GameObject hitFX;

        //cached Transform component
        protected Transform thisTrans;
        //damage value set on the Tower
        protected int damage;
        //network client ID that instantiated the projectile
        protected int ownerId;

        internal Unit target;
        internal Transform targetTrans;
        internal GameObject targetObj;


        //initialize variables
        public virtual void Awake()
        {
            thisTrans = transform;
        }


        /// <summary>
        /// Upon spawn, caches references and sets target.
        /// </summary>
        public void Initialize(Unit unit, int ownerId, int damage)
        {
            target = unit;
            targetTrans = unit.targetPoint;
            targetObj = unit.gameObject;

            this.ownerId = ownerId;
            this.damage = damage;
        }


        /// <summary>
        /// Clears references on despawn.
        /// </summary>
        public virtual void OnDespawn()
        {
            target = null;
            targetTrans = null;
            targetObj = null;
        }
    }


    /// <summary>
    /// Type of projectiles with different logic or damage behavior.
    /// </summary>
    public enum ProjectileType
    {
        /// <summary>
        /// Using a LineRenderer for visualization purposes.
        /// </summary>
        Line,

        /// <summary>
        /// Projectile that actually flies from a tower to the target.
        /// </summary>
        Follow
    }
}
