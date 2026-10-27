using System;
using UnityEngine;
using Xilo.Core;

namespace Xilo.Tools
{
    public readonly struct ToolContact
    {
        public readonly int SampleIndex;
        public readonly Collider Collider;
        public readonly Vector2 Uv;
        public readonly Vector3 Point;
        public readonly Vector3 Normal;

        public ToolContact(int sampleIndex, RaycastHit hit)
        {
            SampleIndex = sampleIndex;
            Collider = hit.collider;
            Uv = hit.textureCoord;
            Point = hit.point;
            Normal = hit.normal;
        }
    }

    // Detects contact between the tool tip samples and paintable surfaces.
    // A short ray is cast along each sample's forward axis, starting slightly behind it.
    [RequireComponent(typeof(XiloTool))]
    public sealed class ToolTipProbe : MonoBehaviour
    {
        const string LogCategory = "Probe";

        [SerializeField] Transform[] tipSamples;
        [SerializeField] LayerMask surfaceMask;
        [SerializeField] float contactDistance = 0.015f;
        [SerializeField] float backOffset = 0.02f;
        [SerializeField] bool requireActiveStep = true;
        [SerializeField] bool logContacts = true;

        XiloTool tool;
        bool[] touching;

        public event Action<ToolContact> Contact;
        public event Action<int> ContactLost;

        public bool IsTouching { get; private set; }
        public float ContactDistance { get => contactDistance; set => contactDistance = Mathf.Max(0f, value); }
        public LayerMask SurfaceMask { get => surfaceMask; set => surfaceMask = value; }

        void Awake()
        {
            tool = GetComponent<XiloTool>();
            if (tipSamples == null || tipSamples.Length == 0)
                tipSamples = new[] { tool.Tip };
            touching = new bool[tipSamples.Length];
        }

        void OnDisable()
        {
            ReleaseAll();
        }

        void FixedUpdate()
        {
            if (!tool.IsHeld || (requireActiveStep && !tool.IsActiveForStep))
            {
                ReleaseAll();
                return;
            }

            bool anyTouching = false;
            for (int i = 0; i < tipSamples.Length; i++)
                anyTouching |= ProbeSample(i);

            SetTouching(anyTouching);
        }

        // Casts the ray of one sample and raises Contact/ContactLost on changes.
        bool ProbeSample(int index)
        {
            Transform sample = tipSamples[index];
            if (sample == null)
                return false;

            Vector3 direction = sample.forward;
            Vector3 origin = sample.position - direction * backOffset;
            bool hitSurface = Physics.Raycast(origin, direction, out RaycastHit hit, backOffset + contactDistance, surfaceMask, QueryTriggerInteraction.Ignore)
                && hit.collider is MeshCollider;

            if (hitSurface)
            {
                touching[index] = true;
                Contact?.Invoke(new ToolContact(index, hit));
                return true;
            }

            if (touching[index])
            {
                touching[index] = false;
                ContactLost?.Invoke(index);
            }
            return false;
        }

        void SetTouching(bool value)
        {
            if (IsTouching == value)
                return;

            IsTouching = value;
            if (logContacts)
                XiloLog.Info(LogCategory, $"{tool.Id} {(value ? "touching" : "left")} surface", this);
        }

        void ReleaseAll()
        {
            for (int i = 0; i < touching.Length; i++)
            {
                if (!touching[i])
                    continue;
                touching[i] = false;
                ContactLost?.Invoke(i);
            }
            SetTouching(false);
        }

        void OnDrawGizmosSelected()
        {
            Transform[] samples = tipSamples != null && tipSamples.Length > 0 ? tipSamples : null;
            if (samples == null)
                return;

            for (int i = 0; i < samples.Length; i++)
            {
                if (samples[i] == null)
                    continue;
                bool isTouching = Application.isPlaying && touching != null && i < touching.Length && touching[i];
                Gizmos.color = isTouching ? Color.green : Color.yellow;
                Vector3 start = samples[i].position - samples[i].forward * backOffset;
                Gizmos.DrawLine(start, samples[i].position + samples[i].forward * contactDistance);
                Gizmos.DrawWireSphere(samples[i].position, 0.004f);
            }
        }
    }
}
