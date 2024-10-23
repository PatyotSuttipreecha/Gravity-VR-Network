/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Splines;
using Unity.Netcode;

namespace TowerDefenseMP
{
    /// <summary>
    /// Logic for an "enemy" unit that is considered a target for towers to destroy.
	/// Contains all necessary references for UI, in-game representation and movement.
    /// </summary>
    public class Unit : MonoBehaviour
    {
        /// <summary>
        /// The point towers should target when shooting at this unit.
        /// </summary>
        public Transform targetPoint;

        /// <summary>
        /// Current health value.
        /// </summary>
        public int health;

        /// <summary>
        /// Maximum health value at game start.
        /// </summary>
        [HideInInspector]
        public int maxHealth;

        /// <summary>
        /// Current shield value absorbing hits.
        /// </summary>
        public int shield;

        /// <summary>
        /// Maximun shield value at game start.
        /// </summary>
        [HideInInspector]
        public int maxShield;

        /// <summary>
        /// Amount of currency to grant when this unit was destroyed.
        /// </summary>
        public int currencyAmount;

        /// <summary>
        /// UI Slider visualizing health value.
        /// </summary>
        public Slider healthSlider;

        /// <summary>
        /// UI Slider visualizing shield value.
        /// </summary>
        public Slider shieldSlider;

        //private NavMeshAgent agent;
        private UnitMove move;


        //get references
        void Awake()
        {
            //saving maximum values for restoring it later
            maxHealth = health;
            maxShield = shield;
            shieldSlider.gameObject.SetActive(shield != 0);
            if (targetPoint == null) targetPoint = transform;

            move = GetComponent<UnitMove>();
        }


        /// <summary>
		/// Reset health and shield values and apply them to the UI.
		/// </summary>
        public void OnSpawn()
        {
            health = Mathf.FloorToInt(maxHealth * WaveManager.GetInstance().difficultyFactor.Value);
            shield = Mathf.FloorToInt(maxShield * WaveManager.GetInstance().difficultyFactor.Value);

            healthSlider.value = (float)health / maxHealth;
            shieldSlider.value = (float)shield / maxShield;
        }


        /// <summary>
		/// Initializes and start movement on the assigned path passed in.
		/// Adds unit to dictionary cache of active units for this path.
		/// </summary>
        public void StartMove(SplineContainer path)
        {
            move.Container = path;
            move.Restart();

            WaveManager.AddPathUnit(move.Container, this);
            StartCoroutine(WaitForPathEnd());
        }


        //wait for the time necessary for the unit to reach the end of the path
        IEnumerator WaitForPathEnd()
        {
            float moveTime = move.Container.CalculateLength() / move.MaxSpeed;
            yield return new WaitForSeconds(moveTime);

            //end of the path was reached, invoke DefensePoint hit
            //this has only an effect on the server since this is server authoritative
            foreach(SplineData<int> data in move.Container.Spline.GetIntDataValues())
            {
                DefensePointManager.GetInstance().defensePoints[data.DefaultValue].Hit();
            }

            //despawn
            PoolManager.Despawn(gameObject);
        }


        /// <summary>
        /// Calculate damage to be taken by a Projectile, triggers score and currency increase on death.
        /// Shield and Health is applied locally, with only the server being able to change networked values.
        /// </summary>
        public void TakeDamage(int damage, int ownerId)
        {
            //reduce shield on hit
            if (shield > 0)
            {
                shield = Mathf.Clamp(shield - damage, 0, maxShield);
                shieldSlider.value = (float)shield / maxShield;
                return;
            }

            //substract health by damage
            health = Mathf.Clamp(health - damage, 0, maxHealth);
            healthSlider.value = (float)health / maxHealth;

            //projectile killed the unit, server authoritative
            if(health <= 0)
            {
                if (NetworkManager.Singleton.IsServer)
                {
                    //the game is already over so don't do anything
                    if (GameManager.GetInstance().IsGameOver()) return;

                    //increase score and currency for that player
                    GameManager.GetInstance().AddCurrency(ownerId, currencyAmount);
                    GameManager.GetInstance().AddScore(ownerId);
                }

                //unit was destroyed, despawn
                PoolManager.Despawn(gameObject);
            }
        }


        /// <summary>
		/// Removes unit from dictionary cache of active units for this path.
		/// </summary>
        public void OnDespawn()
        {
            WaveManager.RemovePathUnit(move.Container, this);
        }
    }
}
