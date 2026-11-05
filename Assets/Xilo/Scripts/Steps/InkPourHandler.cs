using UnityEngine;
using Xilo.Core;
using Xilo.Painting;
using Xilo.Tools;

namespace Xilo.Steps
{
    // Pouring step: ink falls from the pot spout when the held pot is tilted more than
    // pourAngle relative to the glass normal and the spout is above the glass surface.
    // The ink lands while the front of the falling stream reaches the glass: its fall since the pour started is
    // computed from the stream settings and scale (no physics) and compared with the current spout height every step.
    // The splash particles follow the landing point, and random drops are painted around it (splatter).
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
        [Tooltip("The falling stream; its start speed, gravity and scale give the time the ink takes to reach the glass.")]
        [SerializeField] ParticleSystem pourParticles;
        [Tooltip("Splash where the stream hits the glass; follows the landing point while the ink is landing.")]
        [SerializeField] ParticleSystem impactParticles;
        [Tooltip("Extra seconds added to the computed fall time (negative = sooner).")]
        [SerializeField] float landingDelayOffset;
        [Tooltip("Seconds after the pot stops pouring (with enough ink, once the drops landed) before moving on to " +
                 "loading the roller by itself. Negative = wait for Next.")]
        [SerializeField] float autoAdvanceDelay = 0.8f;

        [Header("Splatter")]
        [Tooltip("Drops painted around the landing point per second while the ink is landing. 0 = none.")]
        [SerializeField, Min(0f)] float dropsPerSecond = 8f;
        [Tooltip("Smallest and largest drop, as a fraction of the brush size.")]
        [SerializeField, Min(0.01f)] float dropSizeMin = 0.08f;
        [SerializeField, Min(0.01f)] float dropSizeMax = 0.25f;
        [Tooltip("How far from the landing point the drops fall, in brush sizes.")]
        [SerializeField, Min(0f)] float dropSpread = 1.5f;

        XiloTool pot;
        bool enoughInk;
        bool pouredThisVisit;
        PaintableSurface glass;
        RenderTexture mask;
        Vector2? lastUv;
        bool isPouring;
        bool streamReaching;
        float pourStartTime;
        float fallTime;
        float impactStopTime = float.MaxValue;
        int lastLoggedDecile = -1;
        float pendingDrops;

        void Awake()
        {
            if (pourParticles != null)
                pourParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // The splash lives where the stream lands, not at the spout: detached so the stream doesn't drag or play it.
            // Its local scale is kept as authored (Local scaling mode sizes the particles by it).
            if (impactParticles != null)
            {
                if (pourParticles != null && impactParticles.transform.IsChildOf(pourParticles.transform))
                    impactParticles.transform.SetParent(pourParticles.transform.parent, false);
                impactParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        protected override void OnEnter(StepContext context)
        {
            if (!ResolveReferences())
                return;

            enoughInk = false;
            pouredThisVisit = false;
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
            StopImpactWhenLanded();
            if (!IsActive || pot == null || !pot.IsHeld)
            {
                SetPouring(false, Vector3.zero);
                return;
            }

            if (TryGetPourHit(out RaycastHit hit))
            {
                fallTime = FallTime(hit.distance);
                SetPouring(true, hit.point);
                UpdateStreamReach(hit);
            }
            else
                SetPouring(false, Vector3.zero);
        }

        // Follows the front of the stream every step: it moves with the spout (local stream), so the ink only lands
        // while the front reaches the glass at the current height. Raising the pot lifts it off; lowering it lands sooner.
        void UpdateStreamReach(RaycastHit hit)
        {
            float lifetime = pourParticles != null ? pourParticles.main.startLifetime.constantMax : float.MaxValue;
            float front = FallDistance(Mathf.Min(Time.time - pourStartTime, lifetime));
            bool reaching = front >= hit.distance;

            if (reaching != streamReaching)
            {
                streamReaching = reaching;
                XiloLog.Info(LogCategory, reaching
                    ? $"Ink landed {Time.time - pourStartTime:0.00}s after pouring ({hit.distance:0.000} m fall)"
                    : $"Stream lifted off the glass ({hit.distance:0.000} m, front at {front:0.000} m)", this);
                if (!reaching)
                {
                    lastUv = null;
                    impactStopTime = Time.time;
                }
            }

            if (!reaching)
                return;
            Paint(hit.textureCoord);
            PlayImpact(hit);
        }

        // Distance the first stream particle has fallen after the given time (d = v·t + ½·a·t²).
        float FallDistance(float time)
        {
            if (pourParticles == null)
                return float.MaxValue;
            ParticleSystem.MainModule main = pourParticles.main;
            float speed = AverageOf(main.startSpeed) * StreamScale(main);
            float acceleration = Physics.gravity.magnitude * AverageOf(main.gravityModifier);
            float delayed = Mathf.Max(0f, time - landingDelayOffset);
            return speed * delayed + 0.5f * acceleration * delayed * delayed;
        }

        // Time the first stream particle takes to fall the given distance (no physics: d = v·t + ½·a·t²).
        // The start speed is scaled by the stream's own scale (Local mode: its local scale, Hierarchy: its world
        // scale); gravity is not scaled. Measured on the stream: 4 m/s × 0.05 scale and gravity 0.1 match exactly.
        float FallTime(float distance)
        {
            if (pourParticles == null)
                return Mathf.Max(0f, landingDelayOffset);

            ParticleSystem.MainModule main = pourParticles.main;
            float speed = AverageOf(main.startSpeed) * StreamScale(main);
            float acceleration = Physics.gravity.magnitude * AverageOf(main.gravityModifier);

            float time = acceleration > 1e-4f
                ? (-speed + Mathf.Sqrt(speed * speed + 2f * acceleration * distance)) / acceleration
                : distance / Mathf.Max(1e-4f, speed);
            return Mathf.Max(0f, time + landingDelayOffset);
        }

        float StreamScale(ParticleSystem.MainModule main)
        {
            Transform stream = pourParticles.transform;
            return main.scalingMode switch
            {
                ParticleSystemScalingMode.Local => stream.localScale.z,
                ParticleSystemScalingMode.Hierarchy => stream.lossyScale.z,
                _ => 1f,
            };
        }

        static float AverageOf(ParticleSystem.MinMaxCurve curve)
        {
            return curve.mode switch
            {
                ParticleSystemCurveMode.TwoConstants => (curve.constantMin + curve.constantMax) * 0.5f,
                ParticleSystemCurveMode.Curve => curve.curve.Evaluate(0f) * curve.curveMultiplier,
                ParticleSystemCurveMode.TwoCurves => (curve.curveMin.Evaluate(0f) + curve.curveMax.Evaluate(0f)) * 0.5f * curve.curveMultiplier,
                _ => curve.constant,
            };
        }

        // Keeps the splash on the landing point, facing out of the glass.
        void PlayImpact(RaycastHit hit)
        {
            if (impactParticles == null)
                return;
            impactParticles.transform.SetPositionAndRotation(hit.point, Quaternion.LookRotation(hit.normal));
            impactStopTime = float.MaxValue;
            if (!impactParticles.isEmitting)
                impactParticles.Play(true);
        }

        // After pouring stops, the ink still in the air keeps splashing for the fall time.
        void StopImpactWhenLanded()
        {
            if (impactParticles == null || Time.time < impactStopTime)
                return;
            impactParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            impactStopTime = float.MaxValue;
        }

        // Casts gravity-down from the spout and checks the tilt and height conditions against the glass.
        bool TryGetPourHit(out RaycastHit hit)
        {
            Vector3 spout = pot.Tip.position;
            if (!Physics.Raycast(spout, Vector3.down, out hit, WorkbenchScale.Meters(maxPourDistance), glassMask, QueryTriggerInteraction.Ignore)
                || !PaintableSurface.TryGet(hit.collider, out PaintableSurface touched) || touched != glass)
                return false;

            float tilt = Vector3.Angle(pot.transform.up, hit.normal);
            float height = Vector3.Dot(spout - hit.point, hit.normal);
            return tilt > pourAngle && height > WorkbenchScale.Meters(minHeightAboveGlass);
        }

        void Paint(Vector2 uv)
        {
            if (lastUv.HasValue)
                painter.Line(mask, brush, lastUv.Value, uv);
            else
                painter.Stamp(mask, brush, uv);
            lastUv = uv;
            PaintSplatter(uv);
        }

        // Random drops around the landing point; the count per second is spread over the physics steps.
        void PaintSplatter(Vector2 centerUv)
        {
            pendingDrops += dropsPerSecond * Time.fixedDeltaTime;
            float minSize = Mathf.Min(dropSizeMin, dropSizeMax);
            float maxSize = Mathf.Max(dropSizeMin, dropSizeMax);
            while (pendingDrops >= 1f)
            {
                pendingDrops -= 1f;
                float distance = brush.Size * Random.Range(0.5f, Mathf.Max(0.5f, dropSpread));
                Vector2 uv = centerUv + Random.insideUnitCircle.normalized * distance;
                painter.Stamp(mask, brush, uv, Random.Range(minSize, maxSize));
            }
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
                    // isEmitting, not isPlaying: a stopped stream with drops still falling must restart emitting.
                    if (!pourParticles.isEmitting)
                        pourParticles.Play();
                }
            }

            if (pouring == isPouring)
                return;

            isPouring = pouring;
            XiloLog.Info(LogCategory, pouring ? "Pouring ink" : "Stopped pouring", this);
            if (pouring)
            {
                pourStartTime = Time.time;
                streamReaching = false;
                pouredThisVisit = true;
                CancelAdvance();
                return;
            }

            // Ink still in the air keeps splashing for as long as it takes to land (if the stream was reaching the glass).
            impactStopTime = streamReaching ? Time.time + fallTime : Time.time;
            streamReaching = false;
            lastUv = null;
            pot?.EndUse();
            if (pourParticles != null)
                pourParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            ScheduleAdvance();
        }

        // Moves on to loading the roller once enough ink is on the glass and the pot is upright again
        // (never in the middle of a pour), after the last drops have landed. Coming back to this step with the ink
        // already there doesn't move on until the visitor pours again.
        void ScheduleAdvance()
        {
            if (autoAdvanceDelay < 0f || !pouredThisVisit || isPouring || !enoughInk || IsAdvancePending)
                return;
            float landing = impactStopTime == float.MaxValue ? 0f : Mathf.Max(0f, impactStopTime - Time.time);
            AdvanceAfter(landing + autoAdvanceDelay, () => !isPouring && enoughInk);
        }

        void HandleCoverage(float coverage)
        {
            ReportProgress(coverage);
            enoughInk = coverage >= Definition.MinCoverage;
            ReportCanAdvance(enoughInk);
            ScheduleAdvance();

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
