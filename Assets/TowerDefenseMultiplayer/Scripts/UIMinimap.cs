/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TowerDefenseMP
{
    /// <summary>
	/// Minimap component creating the RenderTexture and updating unit positions in an interval. 
	/// </summary>
    public class UIMinimap : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        /// <summary>
        /// The image where the created RenderTexture should be applied to.
        /// </summary>
        public RawImage renderImage;

        /// <summary>
        /// Indicator rectangle that allows moving the camera via minimap input.
        /// </summary>
        public RectTransform indicatorRect;

        /// <summary>
        /// Camera with specific layer setup that renders the screen for the minimap.
        /// </summary>
        public Camera mapCamera;

        /// <summary>
        /// Resolution of the minimap, higher resolution creates bigger textures.
        /// </summary>
        public int resolution;

        /// <summary>
        /// Interval at which the assigned camera renders the screen again.
        /// </summary>
        public float updateRate;

        private RenderTexture renderTexture;
        private RectTransform imageRect;
        private CameraController camController;
        private Vector2 indicatorPos;


        //get references
        void Awake()
        {
            imageRect = renderImage.GetComponent<RectTransform>();
            camController = Camera.main.GetComponent<CameraController>();
        }


        //create and assign the RenderTexture
        void Start()
        {
            //find corresponding minimap camera
            foreach(Camera camera in Camera.allCameras)
            {
                if(camera.name.Contains("Minimap"))
                {
                    mapCamera = camera;
                    break;
                }
            }

            //make square output
            Rect viewRect = mapCamera.rect;
            viewRect.width = viewRect.height;
            mapCamera.rect = viewRect;

            //create RenderTexture and assign it to image texture
            renderTexture = new RenderTexture(resolution, resolution, 16);
            mapCamera.targetTexture = renderTexture;
            renderImage.texture = renderTexture;
            renderImage.color = Color.white;

            //start render interval
            StartCoroutine(Render());
        }


        //do a render in a loop at the update interval
        IEnumerator Render()
        {
            while (true)
            {
                mapCamera.gameObject.SetActive(true);
                mapCamera.Render();
                mapCamera.gameObject.SetActive(false);

                yield return new WaitForSeconds(updateRate);
            }
        }


        //positions the indicator rectangle on the minimap
        void LateUpdate()
        {
            indicatorPos = camController.GetPositionWithinBoundsNormalized();
            indicatorRect.anchoredPosition = new Vector2(indicatorPos.x * imageRect.rect.width, indicatorPos.y * imageRect.rect.height);
        }


        //returns the percentual location that was clicked on the minimap
        private Vector2 GetPositionNormalized(PointerEventData pointerEventData)
        {
            // get the mouse click/touch position in the local space of the UI element
            RectTransformUtility.ScreenPointToLocalPointInRectangle(imageRect, pointerEventData.position, pointerEventData.pressEventCamera, out Vector2 localPosition);
            return Rect.PointToNormalized(imageRect.rect, localPosition);
        }


        /// <summary>
		/// Logic when clicking on the minimap for moving the player camera.
		/// </summary>
        public void OnPointerDown(PointerEventData pointerEventData)
        {
            camController.SetPositionWithinBounds(GetPositionNormalized(pointerEventData));
        }


        /// <summary>
		/// Logic when dragging on the minimap for moving the player camera.
		/// </summary>
        public void OnDrag(PointerEventData pointerEventData)
        {
            camController.SetPositionWithinBounds(GetPositionNormalized(pointerEventData));
        }
    }
}