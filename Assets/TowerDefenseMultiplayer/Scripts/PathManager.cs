/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Splines;

namespace TowerDefenseMP
{
    /// <summary>
    /// This class holds and provides access to all paths and their native representation of the Unity Splines package.
    /// </summary>
    public class PathManager : MonoBehaviour
    {
        //reference to this script instance
        private static PathManager instance;

        /// <summary>
        /// Whether all SplineContainer components on children should be added automatically.
        /// </summary>
        public bool autoAddChildren;

        /// <summary>
        /// A list of all SplineContainer components placed in the scene.
        /// </summary>
        public List<SplineContainer> splinePaths;

        /// <summary>
        /// Access to the native spline and its path length for each SplineContainer, used by the Unity Jobs System in UnitMove.
        /// </summary>
        public Dictionary<SplineContainer, KeyValuePair<NativeSpline, float>> nativePathsDic = new Dictionary<SplineContainer, KeyValuePair<NativeSpline, float>>();


        //initialize variables
        void Awake()
        {
            instance = this;

            if(autoAddChildren)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    SplineContainer spline = transform.GetChild(i).GetComponent<SplineContainer>();

                    if (!splinePaths.Contains(spline))
                        splinePaths.Add(spline);
                }
            }

            for(int i = 0; i < splinePaths.Count; i++)
            {
                SplinePath<Spline> path = new SplinePath<Spline>(splinePaths[i].Splines);
                float length = path.GetLength();
                NativeSpline native = new NativeSpline(path, splinePaths[i].transform.localToWorldMatrix, Allocator.Persistent);

                nativePathsDic.Add(splinePaths[i], new KeyValuePair<NativeSpline, float>(native, length));
            }
        }


        /// <summary>
        /// Returns a reference to this script instance.
        /// </summary>
        public static PathManager GetInstance()
        {
            return instance;
        }


        //clears references to paths still in the buffer
        void OnDestroy()
        {
            foreach(KeyValuePair<NativeSpline, float> nativePair in nativePathsDic.Values)
            {
                nativePair.Key.Dispose();
            }
        }
    }
}