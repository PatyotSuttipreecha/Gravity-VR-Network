/*  This file is part of the "Tower Defense Multiplayer" project by FLOBUK.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;

namespace TowerDefenseMP
{
    /// <summary>
    /// This component was taken and rewritten from the official SplineAnimate component contained in Unity Splines.
	/// Major changes include removing unnecessary variables and enabling compatibility for the Jobs System and Burst.
    /// </summary>
    public class UnitMove : SplineComponent
    {
        [SerializeField, Tooltip("The target spline to follow.")]
        SplineContainer m_Target;

        [SerializeField, Tooltip("The speed in meters/second that the GameObject animates along the spline at.")]
        float m_MaxSpeed = 10f;

        NativeSpline nativeSpline;
        NativeArray<float> elapsedTimeResult;
        NativeArray<float> normalizedTimeResult;
        NativeArray<Vector3> positionResult;
        NativeArray<Quaternion> rotationResult;

        JobHandle jobHandle;

        float elapsedTime = 0f;
        float m_SplineLength = -1;
        bool m_Playing;
        float m_NormalizedTime;

        /// <summary>The target container of the splines to follow.</summary>
        public SplineContainer Container
        {
            get => m_Target;
            set
            {
                m_Target = value;
            }
        }

        /// <summary> The maxSpeed speed (in Unity units/second) that the Spline traversal will advance in. </summary>
        /// <remarks>
        /// If <see cref="EasingMode"/> is to <see cref="EasingMode.None"/> then the Spline will be traversed at MaxSpeed throughout its length.
        /// Otherwise, the traversal speed will range from 0 to MaxSpeed throughout the Spline's length depending on the easing mode set.
        /// When animation method is set to <see cref="Method.Speed"/> this setter will set the <see cref="MaxSpeed"/> value and automatically recalculate <see cref="Duration"/>,
        /// otherwise, it will have no effect.
        /// </remarks>
        public float MaxSpeed
        {
            get => m_MaxSpeed;
            set
            {
                m_MaxSpeed = Mathf.Max(0f, value);
            }
        }

        /// <summary>
        /// Normalized time of the Spline's traversal. The integer part is the number of times the Spline has been traversed.
        /// The fractional part is the % (0-1) of progress in the current loop.
        /// </summary>
        public float NormalizedTime
        {
            get => m_NormalizedTime;
            set
            {
                m_NormalizedTime = value;
            }
        }

        public Vector3 positionOffset;

        /// <summary> Returns true if object is currently animating along the Spline. </summary>
        public bool IsPlaying => m_Playing;

        void Awake()
        {
            elapsedTimeResult = new NativeArray<float>(1, Allocator.Persistent);
            normalizedTimeResult = new NativeArray<float>(1, Allocator.Persistent);
            positionResult = new NativeArray<Vector3>(1, Allocator.Persistent);
            rotationResult = new NativeArray<Quaternion>(1, Allocator.Persistent);
        }

        bool IsNullOrEmptyContainer()
        {
            if (m_Target == null || m_Target.Splines.Count == 0)
            {
                Debug.LogError("SplineAnimate does not have a valid SplineContainer set.");
                return true;
            }

            return false;
        }


        /// <summary> Begin animating object along the Spline. </summary>
        public void Play()
        {
            if (IsNullOrEmptyContainer())
                return;

            m_Playing = true;
        }


        /// <summary> Pause object's animation along the Spline. </summary>
        public void Pause()
        {
            m_Playing = false;
        }


        /// <summary> Stop the animation and place the object at the beginning of the Spline. </summary>
        /// <param name="autoplay"> If true, the animation along the Spline will start over again. </param>
        public void Restart()
        {
            if (IsNullOrEmptyContainer())
                return;

            KeyValuePair<NativeSpline, float> keyValue = PathManager.GetInstance().nativePathsDic[m_Target];
            nativeSpline = keyValue.Key;
            m_SplineLength = keyValue.Value;

            m_Playing = false;
            NormalizedTime = 0f;
            elapsedTime = 0f;

            if(!jobHandle.IsCompleted)
                jobHandle.Complete();

            normalizedTimeResult[0] = 0;
            elapsedTimeResult[0] = 0;
            positionResult[0] = m_Target.transform.position + (Vector3)m_Target.Splines[0][0].Position + positionOffset;
            rotationResult[0] = Quaternion.LookRotation(m_Target.Splines[0][1].Position);

            Play();
        }


        /// <summary>
        /// Evaluates the animation along the Spline based on deltaTime.
        /// </summary>
        void Update()
        {
            if (!m_Playing || normalizedTimeResult[0] >= 1f)
                return;

            UnitJob job = new UnitJob()
            {
                m_MaxSpeed = MaxSpeed,
                positionOffset = positionOffset,
                deltaTime = Time.deltaTime,

                m_SplineLength = m_SplineLength,
                m_ElapsedTime = elapsedTime,

                nativeSpline = nativeSpline,
                normalizedTimeResult = normalizedTimeResult,
                elapsedTimeResult = elapsedTimeResult,
                positionResult = positionResult,
                rotationResult = rotationResult
            };

            jobHandle = job.Schedule();
        }


        void LateUpdate()
        {
            jobHandle.Complete();

            elapsedTime = elapsedTimeResult[0];
            transform.position = positionResult[0];
            transform.rotation = rotationResult[0];
        }


        void OnDestroy()
        {
            elapsedTimeResult.Dispose(jobHandle);
            normalizedTimeResult.Dispose(jobHandle);
            positionResult.Dispose(jobHandle);
            rotationResult.Dispose(jobHandle);
        }
    }


    [BurstCompile]
    public struct UnitJob : IJob
    {
        public float m_MaxSpeed;
        public Vector3 positionOffset;
        public float deltaTime;

        public float m_SplineLength;
        public float m_ElapsedTime;
        public float m_Duration;
        public float m_NormalizedTime;

        public NativeSpline nativeSpline;
        public NativeArray<float> normalizedTimeResult;
        public NativeArray<float> elapsedTimeResult;
        public NativeArray<Vector3> positionResult;
        public NativeArray<Quaternion> rotationResult;


        /// <summary>
        /// Evaluates the animation along the Spline based on deltaTime.
        /// </summary>
        public void Execute()
        {
            if (m_NormalizedTime >= 1f)
                return;

            CalculateNormalizedTime();
            UpdateTransform();
        }

        void CalculateNormalizedTime()
        {
            m_ElapsedTime += deltaTime;
            m_Duration = m_SplineLength / m_MaxSpeed;

            var t = math.min(m_ElapsedTime, m_Duration);
            t /= m_Duration;

            m_NormalizedTime = math.floor(m_NormalizedTime) + t;
            elapsedTimeResult[0] = m_ElapsedTime;
            normalizedTimeResult[0] = m_NormalizedTime;
        }

        void UpdateTransform()
        {
            EvaluatePositionAndRotation(out var position, out var rotation);

            position += positionOffset;
            positionResult[0] = position;
            rotationResult[0] = rotation;
        }

        void EvaluatePositionAndRotation(out Vector3 position, out Quaternion rotation)
        {
            var t = GetLoopInterpolation();
            position = nativeSpline.EvaluatePosition(t);

            var forward = Vector3.Normalize(nativeSpline.EvaluateTangent(t));
            var up = nativeSpline.EvaluateUpVector(t);
            rotation = Quaternion.LookRotation(forward, up);
        }

        internal float GetLoopInterpolation()
        {
            var t = 0f;
            var normalizedTimeWithOffset = m_NormalizedTime;
            if (math.floor(normalizedTimeWithOffset) == normalizedTimeWithOffset)
                t = math.clamp(normalizedTimeWithOffset, 0f, 1f);
            else
                t = normalizedTimeWithOffset % 1f;

            return t;
        }
    }
}