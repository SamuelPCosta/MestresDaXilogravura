using System.Collections;
using Unity.XR.CoreUtils;
using UnityEngine;
using Xilo.Core;

namespace Xilo.Rig
{
    // Restores the Camera Offset pose authored in the scene after XROrigin applies its own height.
    // In the Editor (simulator) the full pose is kept, including pitch. On a real headset the pitch
    // and authored height are dropped to avoid tilting the horizon and doubling the tracked height.
    [DefaultExecutionOrder(1000)]
    public sealed class RigStartPose : MonoBehaviour
    {
        const string LogCategory = "Rig";

        [SerializeField] XROrigin origin;
        [SerializeField] bool keepPitchOnHeadset;

        Transform cameraOffset;
        Vector3 authoredPosition;
        Quaternion authoredRotation;

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

            if (Application.isEditor)
            {
                origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
                origin.CameraYOffset = authoredPosition.y;
            }
        }

        IEnumerator Start()
        {
            yield return null;
            Apply();
        }

        // Re-applies the authored pose, adapting it when running on a real headset.
        void Apply()
        {
            if (Application.isEditor)
            {
                cameraOffset.localPosition = authoredPosition;
                cameraOffset.localRotation = authoredRotation;
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
    }
}
