using System.Collections.Generic;
using UnityEngine;
using Xilo.Core;
using Xilo.Painting;

namespace Xilo.Tools
{
    // Wood chips curling out of the gouge while it carves the matrix. A random chip mesh grows at the tip (_Reveal)
    // as fast as the tip moves; at a random reveal it breaks off, falls with gravity and fades (_TotalAlpha), then a
    // new one starts. Lifting the gouge off the wood drops the current chip. No physics colliders, no material copies.
    [RequireComponent(typeof(XiloTool), typeof(ToolTipProbe))]
    public sealed class GougeChips : MonoBehaviour
    {
        [Header("Chips")]
        [SerializeField] List<GameObject> chipPrefabs = new();
        [SerializeField] Material chipMaterial;
        [SerializeField] string revealProperty = "_Reveal";
        [SerializeField] string alphaProperty = "_TotalAlpha";
        [Tooltip("Multiplies the chip model's own size (1 = as imported).")]
        [SerializeField] float chipScale = 1f;
        [Tooltip("Chip offset from the tip (tip local space).")]
        [SerializeField] Vector3 positionOffset;
        [Tooltip("Extra chip rotation relative to the tip (degrees), on top of the model's import rotation.")]
        [SerializeField] Vector3 rotationOffset;

        [Header("Growth")]
        [Tooltip("Reveal gained per meter the tip travels while carving.")]
        [SerializeField] float revealPerMeter = 8f;
        [Tooltip("Tip speeds above this count as this (m/s), so a jerk doesn't finish a chip at once.")]
        [SerializeField] float maxSpeed = 0.6f;
        [Tooltip("The chip breaks off at a random reveal between these two values.")]
        [SerializeField] Vector2 breakRevealRange = new(0.5f, 1f);
        [Tooltip("Carving counts as stopped this long after the last contact with the wood (seconds).")]
        [SerializeField] float contactGrace = 0.1f;

        [Header("Fall")]
        [SerializeField] float fadeDuration = 0.35f;
        [SerializeField] float gravityScale = 1f;
        [Tooltip("Share of the tip velocity the chip keeps when it breaks off.")]
        [SerializeField, Range(0f, 1.5f)] float inheritVelocity = 0.6f;
        [Tooltip("Random spin while falling (degrees per second).")]
        [SerializeField] float maxSpin = 360f;

        sealed class FallingChip
        {
            public Transform transform;
            public Renderer[] renderers;
            public Vector3 velocity;
            public Vector3 spin;
            public float age;
        }

        readonly List<FallingChip> falling = new();
        MaterialPropertyBlock block;
        XiloTool tool;
        ToolTipProbe probe;
        int revealId;
        int alphaId;

        Transform growing;
        Renderer[] growingRenderers;
        float reveal;
        float breakAt;
        float lastWoodContact = float.NegativeInfinity;
        Vector3 woodNormal = Vector3.up;
        Vector3 lastTipPosition;
        Vector3 tipVelocity;

        void Awake()
        {
            tool = GetComponent<XiloTool>();
            probe = GetComponent<ToolTipProbe>();
            block = new MaterialPropertyBlock();
            revealId = Shader.PropertyToID(revealProperty);
            alphaId = Shader.PropertyToID(alphaProperty);
            lastTipPosition = tool.Tip.position;
            if (chipPrefabs.Count == 0 || chipMaterial == null)
                XiloLog.Warn("GougeChips", "Chip meshes or material not assigned", this);
        }

        void OnEnable() => probe.Contact += HandleContact;

        void OnDisable()
        {
            probe.Contact -= HandleContact;
            BreakOff();
        }

        void OnDestroy()
        {
            foreach (FallingChip chip in falling)
            {
                if (chip.transform != null)
                    Destroy(chip.transform.gameObject);
            }
        }

        // Only carving counts: the gouge is the step tool and its tip touches the matrix.
        void HandleContact(ToolContact contact)
        {
            if (tool.IsActiveForStep && PaintableSurface.TryGet(contact.Collider, out PaintableSurface surface) && surface.Id == SurfaceId.Matrix)
            {
                lastWoodContact = Time.time;
                woodNormal = contact.Normal;
            }
        }

        void Update()
        {
            TrackTip();
            bool carving = tool.IsHeld && Time.time - lastWoodContact <= contactGrace;

            if (carving)
                Grow();
            else if (growing != null)
                BreakOff();

            UpdateFalling();
        }

        void TrackTip()
        {
            Vector3 tip = tool.Tip.position;
            if (Time.deltaTime > 0f)
                tipVelocity = Vector3.Lerp(tipVelocity, (tip - lastTipPosition) / Time.deltaTime, 0.5f);
            lastTipPosition = tip;
        }

        // Reveals the chip at the tip as fast as the tip travels; breaks it off at its random reveal.
        // A stroke going down the wood (towards the visitor) doesn't lift a chip: the current one drops off and
        // a new one only starts when the stroke goes back up.
        void Grow()
        {
            if (MovingDownTheWood())
            {
                BreakOff();
                return;
            }

            if (growing == null)
                SpawnChip();
            if (growing == null)
                return;

            float speed = Mathf.Min(tipVelocity.magnitude, maxSpeed);
            reveal = Mathf.Min(1f, reveal + speed * Time.deltaTime * revealPerMeter);
            SetProperties(growingRenderers, reveal, 1f);
            if (reveal >= breakAt)
                BreakOff();
        }

        // "Up" the wood is world up projected on the matrix surface, as for the gouge stroke intensity.
        bool MovingDownTheWood()
        {
            Vector3 up = Vector3.ProjectOnPlane(Vector3.up, woodNormal);
            Vector3 motion = Vector3.ProjectOnPlane(tipVelocity, woodNormal);
            return up.sqrMagnitude > 1e-4f && Vector3.Dot(motion, up) < 0f;
        }

        void SpawnChip()
        {
            if (chipPrefabs.Count == 0 || chipMaterial == null)
                return;

            GameObject prefab = chipPrefabs[Random.Range(0, chipPrefabs.Count)];
            if (prefab == null)
                return;

            // Offsets compose with the model's own import rotation and (non-uniform) scale; the chip keeps the
            // model's world size times Chip Scale, whatever the tip scale.
            Transform tip = tool.Tip;
            growing = Instantiate(prefab, tip).transform;
            growing.name = $"{prefab.name} (chip)";
            growing.SetLocalPositionAndRotation(positionOffset, Quaternion.Euler(rotationOffset) * prefab.transform.localRotation);
            Vector3 parentScale = tip.lossyScale;
            Vector3 modelScale = prefab.transform.localScale * chipScale;
            growing.localScale = new Vector3(modelScale.x / parentScale.x, modelScale.y / parentScale.y, modelScale.z / parentScale.z);

            growingRenderers = growing.GetComponentsInChildren<Renderer>();
            foreach (Renderer chipRenderer in growingRenderers)
            {
                chipRenderer.sharedMaterial = chipMaterial;
                chipRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            reveal = 0f;
            breakAt = Random.Range(breakRevealRange.x, breakRevealRange.y);
            SetProperties(growingRenderers, 0f, 1f);
        }

        // Detaches the growing chip: it keeps part of the tip velocity, falls, spins and fades out.
        // A chip that never showed (reveal 0) is just removed.
        void BreakOff()
        {
            if (growing == null)
                return;

            if (reveal <= 0f)
            {
                Destroy(growing.gameObject);
            }
            else
            {
                growing.SetParent(null, true);
                falling.Add(new FallingChip
                {
                    transform = growing,
                    renderers = growingRenderers,
                    velocity = tipVelocity * inheritVelocity,
                    spin = Random.insideUnitSphere * maxSpin,
                });
            }
            growing = null;
            growingRenderers = null;
            reveal = 0f;
        }

        void UpdateFalling()
        {
            float dt = Time.deltaTime;
            for (int i = falling.Count - 1; i >= 0; i--)
            {
                FallingChip chip = falling[i];
                if (chip.transform == null)
                {
                    falling.RemoveAt(i);
                    continue;
                }

                chip.age += dt;
                chip.velocity += Physics.gravity * (gravityScale * dt);
                chip.transform.position += chip.velocity * dt;
                chip.transform.rotation = Quaternion.Euler(chip.spin * dt) * chip.transform.rotation;

                float alpha = 1f - chip.age / Mathf.Max(0.01f, fadeDuration);
                if (alpha <= 0f)
                {
                    Destroy(chip.transform.gameObject);
                    falling.RemoveAt(i);
                    continue;
                }
                SetProperties(chip.renderers, 1f, alpha, keepReveal: true);
            }
        }

        void SetProperties(Renderer[] renderers, float revealValue, float alpha, bool keepReveal = false)
        {
            foreach (Renderer chipRenderer in renderers)
            {
                chipRenderer.GetPropertyBlock(block);
                if (!keepReveal)
                    block.SetFloat(revealId, revealValue);
                block.SetFloat(alphaId, alpha);
                chipRenderer.SetPropertyBlock(block);
            }
        }
    }
}
