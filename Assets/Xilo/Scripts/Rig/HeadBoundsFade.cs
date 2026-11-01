using UnityEngine;
using Xilo.Core;

namespace Xilo.Rig
{
    // Darkens the view when the visitor walks too far from the start spot: past the radius the view fades to black
    // and a message asks them to come back; it clears again as they return. The real head can't be held back
    // (moving the world against it makes people sick), so this only discourages leaving the area.
    // The overlay (HeadBoundsOverlay prefab) sits under the head camera and is drawn on the OnTop layer, so nothing
    // close to the face pokes through it.
    [RequireComponent(typeof(RigStartPose))]
    public sealed class HeadBoundsFade : MonoBehaviour
    {
        const string LogCategory = "HeadBounds";

        [Tooltip("Horizontal distance from the start spot the visitor can walk freely (meters).")]
        [SerializeField, Min(0.1f)] float radius = 2f;
        [Tooltip("Distance past the radius over which the view goes fully dark (meters).")]
        [SerializeField, Min(0.01f)] float fadeDistance = 0.3f;
        [Tooltip("Head-locked overlay (black panel + message), child of the head camera.")]
        [SerializeField] CanvasGroup overlay;
        [Tooltip("Image turned (Z) to point the way back; the image itself points left.")]
        [SerializeField] RectTransform arrow;

        RigStartPose rig;
        bool wasOutside = true;

        void Awake()
        {
            rig = GetComponent<RigStartPose>();
            if (overlay == null)
            {
                XiloLog.Warn(LogCategory, "Overlay not assigned, leaving the area won't darken the view", this);
                enabled = false;
                return;
            }

            overlay.blocksRaycasts = false;
            overlay.interactable = false;
            SetDarkness(0f);
        }

        void OnDisable() => SetDarkness(0f);

        void LateUpdate()
        {
            if (!rig.IsPlaced || rig.Head == null)
                return;

            Vector3 offset = rig.Head.position - rig.StartPoint;
            offset.y = 0f;
            SetDarkness(Mathf.SmoothStep(0f, 1f, (offset.magnitude - radius) / fadeDistance));
            if (wasOutside)
                PointArrowBack(-offset);
        }

        // Turns the arrow towards the start spot as seen from above, relative to where the head faces:
        // up = ahead, down = behind, right/left = to that side. The arrow image points left at rotation 0.
        void PointArrowBack(Vector3 towardsCenter)
        {
            if (arrow == null)
                return;

            Vector3 forward = Vector3.ProjectOnPlane(rig.Head.forward, Vector3.up);
            if (forward.sqrMagnitude < 1e-4f)
                forward = Vector3.ProjectOnPlane(rig.Head.up, Vector3.up);
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            float x = Vector3.Dot(towardsCenter, right);
            float y = Vector3.Dot(towardsCenter, forward);
            float angle = Mathf.Atan2(y, x) * Mathf.Rad2Deg - 180f;
            arrow.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        void SetDarkness(float darkness)
        {
            if (overlay == null)
                return;

            overlay.alpha = darkness;
            bool outside = darkness > 0f;
            if (outside == wasOutside)
                return;
            wasOutside = outside;
            overlay.gameObject.SetActive(outside);
            XiloLog.Info(LogCategory, outside ? "Visitor left the area" : "Visitor is back in the area", this);
        }

        // Scene view: the free area (green) and where the view is fully dark (red), as cylinders around the start spot.
        void OnDrawGizmosSelected()
        {
            var origin = GetComponent<Unity.XR.CoreUtils.XROrigin>();
            Vector3 center = Application.isPlaying && rig != null ? rig.StartPoint
                : origin != null && origin.CameraFloorOffsetObject != null ? origin.CameraFloorOffsetObject.transform.position
                : transform.position;
            center.y = transform.position.y;

            DrawCylinder(center, radius, new Color(0.3f, 1f, 0.4f, 0.9f));
            DrawCylinder(center, radius + fadeDistance, new Color(1f, 0.3f, 0.3f, 0.6f));
        }

        static void DrawCylinder(Vector3 floorCenter, float cylinderRadius, Color color)
        {
            const int Segments = 48;
            const float Height = 2.2f;
            Gizmos.color = color;
            for (int i = 0; i < Segments; i++)
            {
                Vector3 a = floorCenter + Ring(i, Segments, cylinderRadius);
                Vector3 b = floorCenter + Ring(i + 1, Segments, cylinderRadius);
                Gizmos.DrawLine(a, b);
                Gizmos.DrawLine(a + Vector3.up * Height, b + Vector3.up * Height);
                if (i % 6 == 0)
                    Gizmos.DrawLine(a, a + Vector3.up * Height);
            }
        }

        static Vector3 Ring(int index, int segments, float ringRadius)
        {
            float angle = index * Mathf.PI * 2f / segments;
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ringRadius;
        }
    }
}
