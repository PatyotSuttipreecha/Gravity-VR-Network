/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Netcode;
using Unity.Mathematics;

namespace TowerDefenseMP
{
    /// <summary>
    /// Logic for a tower that is the main component for this type of game.
	/// Contains all necessary references for filtering, aiming and shooting at Unit.
    /// </summary>
    public class Tower : NetworkBehaviour
    {
        /// <summary>
        /// Data container describing e.g. damage and range.
        /// </summary>
        public TowerDataScriptableObject data;


        /// <summary>
        /// The exact location where a projectile should be spawned at.
        /// </summary>
        public Transform firePoint;


        /// <summary>
        /// Rotating part, if assigned, to orient to the enemy Unit.
        /// </summary>
        public Transform turret;


        private int buildFrame;
        private Vector3 position;
        private float sqrRange;

        private int maxTargets = 5;
        private List<Unit> allTargets = new List<Unit>(200);

        private Projectile activeProjectile;
        private float nextFire;
        private Coroutine fireCoroutine;
        private WaitForSeconds fireDelay;

        private List<SplineContainer> pathRange = new List<SplineContainer>();
        private List<Unit> targets = new List<Unit>();
        private int targetsCount;
        private int pathRangeCount;
        private int allTargetsCount;


        //get paths in range of this tower, makes it more performant
        //since we can skip to check Units on paths that are out of range
        void Start()
        {
            buildFrame = Time.frameCount % 10;
            position = transform.position;
            sqrRange = data.range * data.range;
            fireDelay = new WaitForSeconds(data.delay);

            foreach(SplineContainer path in PathManager.GetInstance().splinePaths)
            {
                using var native = new NativeSpline(path.Spline, path.transform.localToWorldMatrix);
                float dist = SplineUtility.GetNearestPoint(native, transform.position, out float3 nearest, out float t);
                
                if (dist <= data.range)
                    pathRange.Add(path);
            }

            pathRangeCount = pathRange.Count;
        }


        //running every 10 frames starting with the frame the tower was placed (build frame)
        //logic for filtering and getting valid target goes here
        void Update()
        {
            //run at frame time specified
            if (Time.frameCount % 10 != buildFrame)
                return;

            //remove targets when out of range or despawned already
            targetsCount = targets.Count;
            for(int i = targetsCount - 1; i >= 0; i--)
            {
                if ((targets[i].targetPoint.position - position).sqrMagnitude > sqrRange || !targets[i].gameObject.activeInHierarchy)
                    targets.RemoveAt(i);
            }

            //despawn line projectile when currently targeted Unit was removed above
            if (data.projectileType == ProjectileType.Line && activeProjectile != null && !targets.Contains(activeProjectile.target))
            {
                activeProjectile.target = null;
                activeProjectile = null;
            }

            //maximum amount of targets reached, no need to add more
            if (targets.Count == maxTargets)
                return;

            //reset current available targets for this frame
            allTargets.Clear();
            allTargetsCount = 0;

            //get all possible targets on paths within range
            for (int i = 0; i < pathRangeCount; i++)
                allTargets.AddRange(WaveManager.GetInstance().pathUnitsLast[pathRange[i]]);

            //get count for looping
            //range check against all collected targets
            allTargetsCount = allTargets.Count;
            for (int i = 0; i < allTargetsCount; i++)
            {
                if (targets.Contains(allTargets[i]))
                    continue;

                if ((allTargets[i].targetPoint.position - position).sqrMagnitude < sqrRange)
                {
                    targets.Add(allTargets[i]);

                    if (targets.Count == maxTargets)
                        break;
                }
            }

            //we found a target, spawn projectile(s)
            if (targets.Count > 0)
            {
                switch(data.projectileType)
                {
                    case ProjectileType.Line:
                        if(activeProjectile == null)
                        {
                            SpawnProjectile();
                        }
                        break;

                    case ProjectileType.Follow:
                        if(fireCoroutine == null)
                        {
                            fireCoroutine = StartCoroutine(Fire());
                        }
                        break;
                }
            }
        }


        //rotate turret to target
        void LateUpdate()
        {
            if (targets.Count > 0)
            {
                if (turret != null)
                {
                    Vector3 targetDir = targets[0].targetPoint.position - transform.position;
                    targetDir.y = 0;
                    turret.rotation = Quaternion.LookRotation(targetDir);
                }
            }
        }


        //shot routine running endlessly spawning new projectiles with the specified delay between shots
        //only for Follow projectile types since on Line type there is only one in its full duration
        IEnumerator Fire()
        {
            while (targets.Count > 0)
            {
                if (Time.time < nextFire)
                    yield return new WaitForSeconds(nextFire - Time.time);

                SpawnProjectile();
                nextFire = Time.time + data.delay;

                yield return fireDelay;
            }

            fireCoroutine = null;
        }


        //actual projectile spawn method
        void SpawnProjectile()
        {
            GameObject proj = PoolManager.Spawn(data.projectile, firePoint.position, Quaternion.identity);
            activeProjectile = proj.GetComponent<Projectile>();
            activeProjectile.Initialize(WaveManager.GetInstance().allUnits[targets[0].gameObject], (int)OwnerClientId, data.damage);
        }


        /// <summary>
        /// Overriding OnDestroy to also destroy its projectiles along with this tower.
        /// </summary>
        public override void OnDestroy()
        {
            base.OnDestroy();

            if (activeProjectile != null && activeProjectile.gameObject.scene.isLoaded)
                PoolManager.Despawn(activeProjectile.gameObject);
        }


        //draw editor range gizmo and lines to Units within range
        #if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            foreach(Unit unit in targets)
            {
                UnityEditor.Handles.color = new Color(1, 0, 0);
                UnityEditor.Handles.DrawLine(transform.position, unit.targetPoint.position);
            }

            if (targets.Count != 0)
            {
                UnityEditor.Handles.color = new Color(0, 1, 0);
                UnityEditor.Handles.DrawLine(transform.position, targets[0].targetPoint.position);
            }

            UnityEditor.Handles.color = new Color(0, 1, 0, 0.1f);
            UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.up, data.range);
        }
        #endif
    }
}
