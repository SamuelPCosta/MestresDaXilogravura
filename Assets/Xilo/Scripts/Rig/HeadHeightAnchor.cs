using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.XR;
using Xilo.Core;

namespace Xilo.Rig
{
    // Puts a target (the desk, the menu) a fixed distance below the eyes so every visitor gets the same reach.
    // Runs once the head is tracked at scene start and again after a system recenter (holding the Meta button).
    // Only the target's Y moves (its tilt is kept).
    [DefaultExecutionOrder(1010)]
    public sealed class HeadHeightAnchor : MonoBehaviour
    {
        const string LogCategory = "Rig";

        [SerializeField] Transform trackedCamera;
        [FormerlySerializedAs("desk")]
        [SerializeField] Transform target;
        [Tooltip("Keep the camera-to-target height of the scene layout (measured on Play). Off = use Distance Below Camera.")]
        [SerializeField] bool useSceneDistance = true;
        [Tooltip("Target origin height below the camera (meters). 0 = at eye level. Ignored while Use Scene Distance is on.")]
        [SerializeField] float distanceBelowCamera = 1.15f;
        [Tooltip("Head heights below this are treated as not tracked yet.")]
        [SerializeField] float minTrackedHeight = 0.5f;
        [Tooltip("Gives up waiting for tracking after this many seconds and uses the current height.")]
        [SerializeField] float trackingTimeout = 3f;
        [Tooltip("Children of the target that belong to the room (floor, window): they keep their world pose.")]
        [SerializeField] List<Transform> keepInPlace = new();

        readonly List<XRInputSubsystem> inputSubsystems = new();

        // Reads the authored layout before the rig applies any tracked height (XROrigin moves the camera on Start).
        void Awake()
        {
            if (!useSceneDistance || trackedCamera == null || target == null)
                return;
            distanceBelowCamera = trackedCamera.position.y - target.position.y;
            XiloLog.Info(LogCategory, $"{target.name} keeps the scene distance: {distanceBelowCamera:F3} m below the camera", this);
        }

        void OnEnable()
        {
            SubsystemManager.GetSubsystems(inputSubsystems);
            foreach (XRInputSubsystem subsystem in inputSubsystems)
                subsystem.trackingOriginUpdated += HandleRecentered;
        }

        void OnDisable()
        {
            foreach (XRInputSubsystem subsystem in inputSubsystems)
                subsystem.trackingOriginUpdated -= HandleRecentered;
        }

        IEnumerator Start()
        {
            if (trackedCamera == null || target == null)
            {
                XiloLog.Error(LogCategory, "HeadHeightAnchor needs the camera and the target", this);
                yield break;
            }

            yield return null;
            yield return null;

            float waited = 0f;
            while (trackedCamera.position.y < minTrackedHeight && waited < trackingTimeout)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Calibrate();
        }

        // Moves the target vertically relative to the current head height.
        public void Calibrate()
        {
            try
            {
                var pinned = keepInPlace.Where(item => item != null)
                    .Select(item => (item, item.position, item.rotation)).ToList();

                Vector3 position = target.position;
                float before = position.y;
                position.y = trackedCamera.position.y - distanceBelowCamera;
                target.position = position;

                foreach (var (item, worldPosition, worldRotation) in pinned)
                    item.SetPositionAndRotation(worldPosition, worldRotation);
                Physics.SyncTransforms();
                XiloLog.Info(LogCategory, $"{target.name} height calibrated: head={trackedCamera.position.y:F3} {before:F3} -> {position.y:F3}", this);
            }
            catch (Exception exception)
            {
                XiloLog.Exception(LogCategory, exception, this);
            }
        }

        void HandleRecentered(XRInputSubsystem subsystem)
        {
            if (trackedCamera != null && target != null)
                StartCoroutine(CalibrateNextFrame());
        }

        // The head pose is refreshed by the tracking only on the frame after the recenter.
        IEnumerator CalibrateNextFrame()
        {
            yield return null;
            Calibrate();
        }
    }
}
