using UnityEngine;
using Xilo.Core;
using Xilo.Painting;
using Xilo.Tools;

namespace Xilo.Steps
{
    // Pouring step: ink falls from the pot spout when the held pot is tilted more than
    // pourAngle relative to the glass normal and the spout is above the glass surface.
    public sealed class InkPourHandler : StepHandlerBase
    {
        const string LogCategory = "InkPour";

        [SerializeField] ToolRegistry tools;
        [SerializeField] Workbench workbench;
        [SerializeField] MaskPainter painter;
        [SerializeField] CoverageMeter coverageMeter;
        [SerializeField] BrushProfile brush;

        [Header("Pouring")]
        [Tooltip("Minimum angle (degrees) between the pot up axis and the glass normal to pour.")]
        [SerializeField, Range(0f, 180f)] float pourAngle = 60f;
        [Tooltip("Minimum height of the spout above the glass surface (meters, along the glass normal).")]
        [SerializeField] float minHeightAboveGlass = 0.02f;
        [SerializeField] float maxPourDistance = 2f;
        [SerializeField] LayerMask glassMask;
        [SerializeField] ParticleSystem pourParticles;

        XiloTool pot;
        PaintableSurface glass;
        RenderTexture mask;
        Vector2? lastUv;
        bool isPouring;
        int lastLoggedDecile = -1;

        void Awake()
        {
            if (pourParticles != null)
                pourParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        protected override void OnEnter(StepContext context)
        {
            if (!ResolveReferences())
                return;

            coverageMeter.Track(mask, Definition.CoverageTarget, HandleCoverage);
            lastLoggedDecile = -1;
            ReportCanAdvance(false);
        }

        protected override void OnExit()
        {
            SetPouring(false, Vector3.zero);
            coverageMeter?.Stop();
        }

        public override void ClearProgress()
        {
            RenderTexture target = mask != null ? mask : ResolveMask();
            if (target != null && painter != null)
                painter.Clear(target);
            lastUv = null;
            lastLoggedDecile = -1;
            ReportProgress(0f);
            ReportCanAdvance(false);
            coverageMeter?.Refresh();
        }

        public override void DebugFill()
        {
            if (mask == null || painter == null)
                return;
            painter.Fill(mask);
            coverageMeter.Refresh();
        }

        void FixedUpdate()
        {
            if (!IsActive || pot == null || !pot.IsHeld)
            {
                SetPouring(false, Vector3.zero);
                return;
            }

            if (TryGetPourHit(out RaycastHit hit))
            {
                Paint(hit.textureCoord);
                SetPouring(true, hit.point);
            }
            else
                SetPouring(false, Vector3.zero);
        }

        // Casts gravity-down from the spout and checks the tilt and height conditions against the glass.
        bool TryGetPourHit(out RaycastHit hit)
        {
            Vector3 spout = pot.Tip.position;
            if (!Physics.Raycast(spout, Vector3.down, out hit, maxPourDistance, glassMask, QueryTriggerInteraction.Ignore)
                || !PaintableSurface.TryGet(hit.collider, out PaintableSurface touched) || touched != glass)
                return false;

            float tilt = Vector3.Angle(pot.transform.up, hit.normal);
            float height = Vector3.Dot(spout - hit.point, hit.normal);
            return tilt > pourAngle && height > minHeightAboveGlass;
        }

        void Paint(Vector2 uv)
        {
            if (lastUv.HasValue)
                painter.Line(mask, brush, lastUv.Value, uv);
            else
                painter.Stamp(mask, brush, uv);
            lastUv = uv;
        }

        // Starts/stops the pouring feedback (sound through the pot, stream particles at the spout).
        void SetPouring(bool pouring, Vector3 hitPoint)
        {
            if (pouring)
            {
                pot.BeginUse(hitPoint);
                if (pourParticles != null)
                {
                    pourParticles.transform.SetPositionAndRotation(pot.Tip.position, Quaternion.LookRotation(Vector3.down));
                    if (!pourParticles.isPlaying)
                        pourParticles.Play();
                }
            }

            if (pouring == isPouring)
                return;

            isPouring = pouring;
            XiloLog.Info(LogCategory, pouring ? "Pouring ink" : "Stopped pouring", this);
            if (pouring)
                return;

            lastUv = null;
            pot?.EndUse();
            if (pourParticles != null)
                pourParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        void HandleCoverage(float coverage)
        {
            ReportProgress(coverage);
            ReportCanAdvance(coverage >= Definition.MinCoverage);

            int decile = Mathf.FloorToInt(coverage * 10f);
            if (decile == lastLoggedDecile)
                return;
            lastLoggedDecile = decile;
            XiloLog.Info(LogCategory, $"Coverage {Id}: {coverage:P0} (min {Definition.MinCoverage:P0})", this);
        }

        bool ResolveReferences()
        {
            if (tools == null || workbench == null || painter == null || coverageMeter == null || brush == null)
            {
                XiloLog.Error(LogCategory, "Missing references", this);
                return false;
            }

            if (!tools.TryGet(Definition.ToolId, out pot))
            {
                XiloLog.Error(LogCategory, $"Tool {Definition.ToolId} not found", this);
                return false;
            }

            glass = workbench.Get(Definition.Surface);
            mask = ResolveMask();
            if (glass == null || mask == null)
            {
                XiloLog.Error(LogCategory, $"Surface {Definition.Surface} or mask {Definition.MaskProperty} not found", this);
                return false;
            }
            return true;
        }

        RenderTexture ResolveMask()
        {
            PaintableSurface target = workbench != null ? workbench.Get(Definition.Surface) : null;
            return target != null ? target.GetMask(Definition.MaskProperty) : null;
        }
    }
}
