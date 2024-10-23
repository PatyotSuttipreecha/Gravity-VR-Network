/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TowerDefenseMP
{
    /// <summary>
	/// Manages loading of the separate UI scene and applying cross-scene UI references.
	/// </summary>
    public class UIManager : MonoBehaviour
    {
        //name of the scene that should be loaded
        private const string sceneNameUI = "Map_UI";

        //reference to this script instance
        private static UIManager instance;

        /// <summary>
		/// Reference to the UIGame component in the loaded UI scene.
		/// </summary>
        [HideInInspector]
        public UIGame ui;

        /// <summary>
        /// Minimap texture that is different for every map.
        /// </summary>
        public Texture mapTexture;


        //get references
        void Awake()
        {
            instance = this;

            //load scene immediately
            StartCoroutine(LoadScene());
        }


        //load UI scene and assign UIGame references
        IEnumerator LoadScene()
        {
            //start loading the scene additively
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneNameUI, LoadSceneMode.Additive);
            //wait until the level finish loading
            while (!asyncLoad.isDone)
                yield return null;

            //takeover UIGame references
            ui = FindObjectOfType<UIGame>();
            ui.mapImage.texture = mapTexture;
        }


        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static UIManager GetInstance()
        {
            return instance;
        }
    }
}