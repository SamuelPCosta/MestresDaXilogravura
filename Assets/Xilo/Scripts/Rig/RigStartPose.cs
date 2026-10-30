using System;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;
using Xilo.Core;

namespace Xilo.Rig
{
    // Restores the Camera Offset pose authored in the scene after XROrigin applies its own height.
    // The offset is always kept level: in the Editor the authored pitch is handed to the simulated head
    // (EditorOnlySimulator); on a real headset the authored height is dropped to avoid doubling the tracked height,
    // and the floor origin is forced so the camera sits at the visitor's real head height (HeadHeightAnchor relies on it).
    // Then the rig is turned and slid so the visitor starts where the scene camera was, facing the same way
    // (the desk, the menu), wherever they stand in the room. Repeated after a system recenter.
    [DefaultExecutionOrder(1000)]
    public sealed class RigStartPose : MonoBehaviour
    {
        const string LogCategory = "Rig";

        [SerializeField] XROrigin origin;
        [SerializeField] bool keepPitchOnHeadset;
        [Tooltip("Start at the scene camera's spot, facing its direction, wherever the visitor stands.")]
        [SerializeField] bool faceAuthoredDirection = true;
        [Tooltip("Head heights below this are treated as not tracked yet.")]
        [SerializeField] float minTrackedHeight = 0.5f;
        [Tooltip("Gives up waiting for tracking after this many seconds.")]
        [SerializeField] float trackingTimeout = 3f;

        Transform cameraOffset;
        Vector3 authoredPosition;
        Quaternion authoredRotation;
        Vector3 startPoint;
        float startYaw;
        readonly List<XRInputSubsystem> inputSubsystems = new();

        // Downward look authored on the Camera Offset (degrees, positive = down); the Editor simulator starts the head with it.
        public float AuthoredPitch => Mathf.DeltaAngle(0f, authoredRotation.eulerAngles.x);

        void Awake()
        {
            if (origin == null)
                origin = GetComponent<XROrigin>();

            if (origin == null || origin.CameraFloorOffsetObject == null)
            {
                XiloLog.Error(LogCategory, "XROrigin or Camera Offset not found", this);
                enabled = false;
                return;
            }

            cameraOffset = origin.CameraFloorOffsetObject.transform;
            authoredPosition = cameraOffset.localPosition;
            authoredRotation = cameraOffset.localRotation;
            startPoint = cameraOffset.position;
            startYaw = cameraOffset.eulerAngles.y;

            if (Application.isEditor)
            {
                origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
                origin.CameraYOffset = authoredPosition.y;
            }
            else
            {
                origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            }
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
            yield return null;
            Apply();
            yield return WaitForTracking();
            Recenter();
        }

        // Re-applies the authored pose, adapting it when running on a real headset.
        void Apply()
        {
            if (Application.isEditor)
            {
                // Kept level: the simulator moves the head on the plane of the camera's parent, so a pitched
                // offset would turn walking into a diagonal. The authored pitch goes to the simulated head instead.
                cameraOffset.localPosition = authoredPosition;
                cameraOffset.localRotation = Quaternion.Euler(0f, authoredRotation.eulerAngles.y, 0f);
            }
            else
            {
                cameraOffset.localPosition = new Vector3(authoredPosition.x, cameraOffset.localPosition.y, authoredPosition.z);
                cameraOffset.localRotation = keepPitchOnHeadset
                    ? authoredRotation
                    : Quaternion.Euler(0f, authoredRotation.eulerAngles.y, 0f);
            }

            XiloLog.Info(LogCategory, $"Camera Offset pose applied: pos={cameraOffset.localPosition} euler={cameraOffset.localEulerAngles}", this);
        }

        IEnumerator WaitForTracking()
        {
            float waited = 0f;
            while (origin.Camera.transform.position.y < minTrackedHeight && waited < trackingTimeout)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // Turns the rig around the head to the authored yaw, then slides it so the head is over the authored spot (height kept).
        public void Recenter()
        {
            if (!faceAuthoredDirection)
                return;

            try
            {
                Transform head = origin.Camera.transform;
                origin.RotateAroundCameraUsingOriginUp(Mathf.DeltaAngle(head.eulerAngles.y, startYaw));
                origin.MoveCameraToWorldLocation(new Vector3(startPoint.x, head.position.y, startPoint.z));
                XiloLog.Info(LogCategory, $"Facing start: head={head.position} yaw={head.eulerAngles.y:F1} (target {startYaw:F1})", this);
            }
            catch (Exception exception)
            {
                XiloLog.Exception(LogCategory, exception, this);
            }
        }

        void HandleRecentered(XRInputSubsystem subsystem)
        {
            XiloLog.Info(LogCategory, "Tracking origin recentered", this);
            StartCoroutine(RecenterNextFrame());
        }

        // The head pose is refreshed by the tracking only on the frame after the recenter.
        IEnumerator RecenterNextFrame()
        {
            yield return null;
            Recenter();
        }
    }
}
