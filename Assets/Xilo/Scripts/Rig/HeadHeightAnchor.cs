using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;
using Xilo.Core;

namespace Xilo.Rig
{
    // Puts the targets (the desk, the menu, the walls) a fixed distance below the eyes so every visitor gets the same reach.
    // Runs once the head is tracked at scene start and again after a system recenter (holding the Meta button).
    // Only their Y moves (tilt kept), all by the same amount so the layout between them never changes.
    // The first target is the reference for the distance below the camera.
    [DefaultExecutionOrder(1010)]
    public sealed class HeadHeightAnchor : MonoBehaviour
    {
        const string LogCategory = "Rig";

        [SerializeField] Transform trackedCamera;
        [Tooltip("Moved together; the first one (the desk, the menu) sets the height.")]
        [SerializeField] List<Transform> targets = new();
        [Tooltip("Keep the camera-to-first-target height of the scene layout (measured on Play). Off = use Distance Below Camera.")]
        [SerializeField] bool useSceneDistance = true;
        [Tooltip("Target origin height below the camera (meters). 0 = at eye level. Ignored while Use Scene Distance is on.")]
        [SerializeField] float distanceBelowCamera = 1.15f;
        [Tooltip("Head heights below this are treated as not tracked yet.")]
        [SerializeField] float minTrackedHeight = 0.5f;
        [Tooltip("Gives up waiting for tracking after this many seconds and uses the current height.")]
        [SerializeField] float trackingTimeout = 3f;
        [Tooltip("Children of the targets that belong to the room (floor, window): they keep their world pose.")]
        [SerializeField] List<Transform> keepInPlace = new();

        readonly List<XRInputSubsystem> inputSubsystems = new();

        Transform Reference => targets.Count > 0 ? targets[0] : null;

        // Reads the authored layout before the rig applies any tracked height (XROrigin moves the camera on Start).
        void Awake()
        {
            if (!useSceneDistance || trackedCamera == null || Reference == null)
                return;
            distanceBelowCamera = trackedCamera.position.y - Reference.position.y;
            XiloLog.Info(LogCategory, $"{Reference.name} keeps the scene distance: {distanceBelowCamera:F3} m below the camera", this);
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
            if (trackedCamera == null || Reference == null)
            {
                XiloLog.Error(LogCategory, "HeadHeightAnchor needs the camera and at least one target", this);
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

        // Moves every target vertically by the amount that puts the first one at its distance below the head.
        public void Calibrate()
        {
            try
            {
                var pinned = keepInPlace.Where(item => item != null)
                    .Select(item => (item, item.position, item.rotation)).ToList();

                float before = Reference.position.y;
                float deltaY = trackedCamera.position.y - distanceBelowCamera - before;
                foreach (Transform target in targets.Where(item => item != null))
                    target.position += Vector3.up * deltaY;

                foreach (var (item, worldPosition, worldRotation) in pinned)
                    item.SetPositionAndRotation(worldPosition, worldRotation);
                Physics.SyncTransforms();
                XiloLog.Info(LogCategory, $"{targets.Count} targets height calibrated: head={trackedCamera.position.y:F3} {Reference.name} {before:F3} -> {Reference.position.y:F3}", this);
            }
            catch (Exception exception)
            {
                XiloLog.Exception(LogCategory, exception, this);
            }
        }

        void HandleRecentered(XRInputSubsystem subsystem)
        {
            if (trackedCamera != null && Reference != null)
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
