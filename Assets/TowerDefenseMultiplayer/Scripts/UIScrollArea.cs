/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TowerDefenseMP
{
    /// <summary>
    /// Area that makes the camera scroll in a specific direction when hovering or touching the corresponding UI element.
    /// </summary>
    public class UIScrollArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        /// <summary>
        /// The direction this component should apply movement to.
        /// </summary>
        public Vector2 direction;

        //reference to the camera script
        private CameraController camController;


        //get camera reference
        void Awake()
        {
            camController = Camera.main.GetComponent<CameraController>();

            //disable edge elements on standalone build. actually disabling
			//would disable the component logic too, so we just make it transparent
            #if UNITY_STANDALONE || UNITY_WEBGL
            GetComponent<Image>().color = new Color(1,1,1,0);
            #endif
        }



        /// <summary>
        /// Logic when entering the element by hovering in.
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            /*
            #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
            camController.ApplyScrollDirection(direction);
            #endif
            */
        }


        /// <summary>
        /// Logic when exiting the element by hovering out.
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            /*
            #if UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL
            camController.ApplyScrollDirection(-direction);
            #endif
            */
        }


        /// <summary>
        /// Logic when starting to click the element.
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            #if !UNITY_EDITOR && !UNITY_STANDALONE && !UNITY_WEBGL
            camController.ApplyScrollDirection(direction);
            #endif
        }


        /// <summary>
        /// Logic when releasing the click on the element.
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            #if !UNITY_EDITOR && !UNITY_STANDALONE && !UNITY_WEBGL
            camController.ApplyScrollDirection(-direction);
            #endif
        }
    }
}
