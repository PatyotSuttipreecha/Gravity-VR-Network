/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TowerDefenseMP
{
    /// <summary>
    /// Button component to upgrade a tower.
    /// </summary>
    public class UIButtonCooldown : MonoBehaviour
    {
        private Button button;

        /// <summary>
        /// Reference to the image being animated.
        /// </summary>
        public Image radialImage;

        /// <summary>
        /// Seconds the cooldown should take from start to finish.
        /// </summary>
        public int delaySeconds;


        //get references
        void Awake()
        {
            button = GetComponent<Button>();
        }


        //start cooldown immediately
        void OnEnable()
        {
            StopAllCoroutines();
            StartCooldown();
        }


        /// <summary>
        /// Creates a new cooldown routine.
        /// </summary>
        public void StartCooldown()
        {
            StartCoroutine(Delay());
        }


        //delay routine that animates the radial image
        //over the delay in seconds specified
        IEnumerator Delay()
        {
            button.interactable = false;

            float timerValue = 0;
            while (timerValue <= 1f)
            {
                radialImage.fillAmount = timerValue;
                timerValue += Time.deltaTime / delaySeconds;
                yield return null;
            }

            button.interactable = true;
        }
    }
}
