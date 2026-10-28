using UnityEngine;
using Xilo.Core;

namespace Xilo.Rig
{
    // Soft head look limits: sits between the Camera Offset and the camera and counter-rotates the view
    // so looking further than a soft start angle gets progressively "harder", approaching (never hitting) the limit.
    // Angles are relative to the rig rest pose. Only the camera is affected; controllers are siblings.
    [DefaultExecutionOrder(1100)]
    public sealed class HeadLookLimiter : MonoBehaviour
    {
        const string LogCategory = "HeadLimiter";

        [SerializeField] Transform trackedCamera;

        [Header("LOOK LIMITS (degrees from the rest pose)")]
        [SerializeField, Range(1f, 90f)] float upLimit = 30f;
        [SerializeField, Range(1f, 90f)] float downLimit = 45f;
        [SerializeField, Range(1f, 180f)] float sideLimit = 60f;
        [Tooltip("Fraction of each limit where the resistance starts (1:1 before it, progressively harder after it).")]
        [SerializeField, Range(0f, 0.95f)] float softStart = 0.5f;

        Vector3 restLocalPosition;

        void Awake()
        {
            if (trackedCamera == null || trackedCamera.parent != transform)
            {
                XiloLog.Error(LogCategory, "Tracked camera must be a direct child of the limiter", this);
                enabled = false;
                return;
            }
            restLocalPosition = transform.localPosition;
        }

        void OnEnable() => Application.onBeforeRender += Apply;

        void OnDisable()
        {
            Application.onBeforeRender -= Apply;
            transform.localRotation = Quaternion.identity;
            transform.localPosition = restLocalPosition;
        }

        void LateUpdate() => Apply();

        // Reads the raw head yaw/pitch, softens them and rotates this pivot around the head so its position is kept.
        void Apply()
        {
            Quaternion head = trackedCamera.localRotation;
            Vector3 forward = head * Vector3.forward;
            float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Asin(Mathf.Clamp(forward.y, -1f, 1f)) * Mathf.Rad2Deg;

            float softYaw = Soften(yaw, sideLimit);
            float softPitch = Soften(pitch, pitch < 0f ? upLimit : downLimit);

            Quaternion raw = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(pitch, 0f, 0f);
            Quaternion soft = Quaternion.Euler(0f, softYaw, 0f) * Quaternion.Euler(softPitch, 0f, 0f);
            Quaternion correction = soft * Quaternion.Inverse(raw);

            Vector3 headLocal = trackedCamera.localPosition;
            transform.localRotation = correction;
            transform.localPosition = restLocalPosition + headLocal - correction * headLocal;
        }

        // Identity up to the soft start, then an exponential ease that tends to the limit (slope stays continuous).
        float Soften(float angle, float limit)
        {
            float start = limit * softStart;
            float magnitude = Mathf.Abs(angle);
            if (magnitude <= start)
                return angle;

            float range = limit - start;
            float eased = start + range * (1f - Mathf.Exp(-(magnitude - start) / range));
            return Mathf.Sign(angle) * eased;
        }
    }
}
