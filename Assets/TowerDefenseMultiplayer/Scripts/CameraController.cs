/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using System.Collections;
using System.Drawing;
using UnityEngine;

namespace TowerDefenseMP
{
    /// <summary>
	/// Controller handling camera input for the local player.
	/// </summary>
    public class CameraController : MonoBehaviour
    {
        /// <summary>
        /// Scroll speed when moving, hovering or touching screen edges.
        /// </summary>
        public int scrollSpeed = 25;

        /// <summary>
        /// The clamped distance in the x-z plane to the target.
        /// </summary>
        public float distance = 10.0f;

        /// <summary>
        /// The clamped height the camera should be above the target.
        /// </summary>
        public float height = 5.0f;

        /// <summary>
        /// Area the camera can move within and is blocked by.
        /// </summary>
        public BoxCollider area;

        private Vector2 scrollDir;
        private Vector3 targetPos;
        private Transform camTrans;
        private Vector3 deltaPos;
        private float offset;


        //set initial camera starting position
        IEnumerator Start()
        {
            camTrans = transform;
            targetPos = area.center;

            //wait until the network is ready
            while (GameManager.GetInstance() == null || GameManager.GetInstance().localPlayer.Equals(default(PlayerStruct)))
                yield return null;

            //get our local player object transform for this client
            Vector3 playerPos = GameManager.GetInstance().playerSpawns[GameManager.GetInstance().localPlayer.thisColor].position;
            //don't let the starting position be outside the camera area
            targetPos = area.ClosestPoint(playerPos);
            targetPos.y = playerPos.y;

            //the AudioListener for this scene is not attached directly to this camera,
            //but to a separate gameobject parented to the camera. This is because the
            //camera is usually positioned above the player, however the AudioListener
            //should consider audio clips from the position of the player in 3D space.
            //Here we position the AudioListener child object at the target position.
            Transform listener = GetComponentInChildren<AudioListener>().transform;
            listener.position = targetPos;
        }


        //always clamp the camera position and rotation to look at the target position
        void FollowTarget()
        {
            //convert the camera's transform angle into a rotation
            Quaternion currentRotation = Quaternion.Euler(0, camTrans.eulerAngles.y, 0);

            //set the position of the camera on the x-z plane to:
            //distance units behind the target, height units above the target
            Vector3 pos = targetPos;
            pos -= currentRotation * Vector3.forward * Mathf.Abs(distance);
            pos.y = targetPos.y + Mathf.Abs(height);
            camTrans.position = pos;

            //look at the target
            camTrans.LookAt(targetPos);

            //clamp distance
            camTrans.position = targetPos - (camTrans.forward * Mathf.Abs(distance));
        }


        //handles player input
        void LateUpdate()
        {
            FollowTarget();

            //outside of game view, do not move further
            if (Input.mousePosition.x > Screen.width || Input.mousePosition.x < 0 || Input.mousePosition.y > Screen.height || Input.mousePosition.y < 0)
                return;

            //get moving direction
            offset = scrollSpeed * Time.unscaledDeltaTime;
            deltaPos = Vector3.zero;

            //apply offset to delta position based on direction
            if (Input.GetKey(KeyCode.D) || scrollDir.x > 0) deltaPos.x += offset;
            if (Input.GetKey(KeyCode.A) || scrollDir.x < 0) deltaPos.x -= offset;
            if (Input.GetKey(KeyCode.W) || scrollDir.y > 0) deltaPos.z += offset;
            if (Input.GetKey(KeyCode.S) || scrollDir.y < 0) deltaPos.z -= offset;

            //if within area, move target position by delta
            if(IsPointWithinArea(targetPos + deltaPos))
                targetPos += deltaPos;
        }


        //check whether point is within collider area
        bool IsPointWithinArea(Vector3 point)
        {
            return area.ClosestPoint(point) == point;
        }


        /// <summary>
        /// Applies scroll input to the scrollDir variable.
		/// This is used by the UIScrollArea component for screen edges.
        /// </summary>
        public void ApplyScrollDirection(Vector2 dir)
        {
            scrollDir += dir;

            scrollDir.x = Mathf.Clamp(scrollDir.x, -1, 1);
            scrollDir.y = Mathf.Clamp(scrollDir.y, -1, 1);
        }


        /// <summary>
        /// Sets the target position on the area based on a percentual location.
		/// This receives the input from UIMinimap when moving the indicator rectangle around.
        /// </summary>
        public void SetPositionWithinBounds(Vector2 value)
        {
            targetPos = new Vector3(Mathf.Lerp(area.bounds.min.x, area.bounds.max.x, value.x), targetPos.y, Mathf.Lerp(area.bounds.min.z, area.bounds.max.z, value.y));
        }


        /// <summary>
        /// Returns the percentual location of the camera within its area.
		/// This is used by the UIMinimap component for the indicator rectangle.
        /// </summary>
        public Vector2 GetPositionWithinBoundsNormalized()
        {
            return new Vector2((targetPos.x - area.bounds.min.x) / (area.bounds.max.x - area.bounds.min.x),
                               (targetPos.z - area.bounds.min.z) / (area.bounds.max.z - area.bounds.min.z));
        }
    }
}
