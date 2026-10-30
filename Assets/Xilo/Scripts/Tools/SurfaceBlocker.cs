using System.Collections.Generic;
using UnityEngine;

namespace Xilo.Tools
{
    // A slab tools can't go through (the matrix, the glass). Its top face is the side of its thinnest axis that
    // faces up; held tools are kept on top of it by SurfaceGuardTransformer. Add or remove the component freely.
    [RequireComponent(typeof(MeshCollider))]
    public sealed class SurfaceBlocker : MonoBehaviour
    {
        static readonly List<SurfaceBlocker> active = new();

        [Tooltip("Blocks only while the hand holding the tool is above the top face or at most this deep under it (meters); a hand further down (e.g. under the desk) is left alone.")]
        [SerializeField, Min(0f)] float maxHandDepth = 0.15f;
        [Tooltip("Extra border around the slab that still blocks (meters).")]
        [SerializeField, Min(0f)] float margin = 0.005f;

        MeshCollider meshCollider;
        Bounds localBounds;
        int thinAxis;

        public static IReadOnlyList<SurfaceBlocker> Active => active;

        void Awake()
        {
            meshCollider = GetComponent<MeshCollider>();
            localBounds = meshCollider.sharedMesh != null ? meshCollider.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one);
            Vector3 size = localBounds.size;
            thinAxis = size.x <= size.y && size.x <= size.z ? 0 : size.y <= size.z ? 1 : 2;
        }

        void OnEnable() => active.Add(this);

        void OnDisable() => active.Remove(this);

        // World direction the top face looks at.
        public Vector3 Normal => transform.TransformDirection(LocalAxis * TopSign).normalized;

        Vector3 LocalAxis
        {
            get
            {
                Vector3 axis = Vector3.zero;
                axis[thinAxis] = 1f;
                return axis;
            }
        }

        float TopSign => transform.TransformDirection(LocalAxis).y >= 0f ? 1f : -1f;

        // True when a tool held at this hand position should be kept on top (hand above or only slightly under).
        public bool Guards(Vector3 handPosition)
        {
            return SignedDepth(handPosition) <= maxHandDepth;
        }

        // How far (meters, along Normal) a point is under the top face, inside the slab outline; 0 if it isn't.
        public float Penetration(Vector3 worldPoint)
        {
            float depth = SignedDepth(worldPoint);
            return depth > 0f && IsOverSlab(worldPoint) ? depth : 0f;
        }

        // Positive under the top face, negative above it (meters along Normal).
        float SignedDepth(Vector3 worldPoint)
        {
            float sign = TopSign;
            Vector3 local = transform.InverseTransformPoint(worldPoint);
            float top = localBounds.center[thinAxis] + sign * localBounds.extents[thinAxis];
            return (top - local[thinAxis]) * sign * Mathf.Abs(transform.lossyScale[thinAxis]);
        }

        bool IsOverSlab(Vector3 worldPoint)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint);
            for (int i = 0; i < 3; i++)
            {
                if (i != thinAxis && Mathf.Abs(local[i] - localBounds.center[i]) > localBounds.extents[i] + margin / Mathf.Abs(transform.lossyScale[i]))
                    return false;
            }
            return true;
        }
    }
}
