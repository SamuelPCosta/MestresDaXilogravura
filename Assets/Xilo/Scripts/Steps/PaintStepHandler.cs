using System.Collections.Generic;
using UnityEngine;
using Xilo.Core;
using Xilo.Painting;
using Xilo.Tools;

namespace Xilo.Steps
{
    // Generic painting step: while the step tool touches the step surface, strokes are painted
    // into the step mask and the coverage of the art target drives the Next button.
    public class PaintStepHandler : StepHandlerBase
    {
        const string LogCategory = "PaintStep";

        [SerializeField] ToolRegistry tools;
        [SerializeField] Workbench workbench;
        [SerializeField] MaskPainter painter;
        [SerializeField] CoverageMeter coverageMeter;
        [SerializeField] BrushProfile brush;

        readonly Dictionary<int, Vector2> lastUvBySample = new();
        readonly Dictionary<int, Vector3> lastPointBySample = new();
        readonly Dictionary<int, float> lastWhiteBySample = new();
        readonly Dictionary<int, Vector2> smoothedUvBySample = new();
        Vector2 paintedUv;
        XiloTool tool;
        ToolTipProbe probe;
        PaintableSurface surface;
        RenderTexture mask;
        int lastLoggedDecile = -1;
        float lastWrongSurfaceLog;
        Vector2? lastRectangleUv;
        float lastRectangleTime = -1f;

        public BrushProfile Brush => brush;
        protected RenderTexture Mask => mask;
        protected XiloTool Tool => tool;

        protected override void OnEnter(StepContext context)
        {
            if (!ResolveReferences())
                return;

            probe.Contact += HandleContact;
            probe.ContactLost += HandleContactLost;
            coverageMeter.Track(mask, Definition.CoverageTarget, HandleCoverage, brush.PaintedThreshold);
            lastLoggedDecile = -1;
            ReportCanAdvance(false);
        }

        protected override void OnExit()
        {
            if (probe != null)
            {
                probe.Contact -= HandleContact;
                probe.ContactLost -= HandleContactLost;
            }
            coverageMeter?.Stop();
            tool?.EndUse();
            lastUvBySample.Clear();
            lastPointBySample.Clear();
            lastWhiteBySample.Clear();
            smoothedUvBySample.Clear();
            lastRectangleUv = null;
        }

        public override void ClearProgress()
        {
            RenderTexture target = mask != null ? mask : ResolveMask();
            if (target != null && painter != null)
                painter.Clear(target);
            lastUvBySample.Clear();
            lastPointBySample.Clear();
            lastWhiteBySample.Clear();
            smoothedUvBySample.Clear();
            lastRectangleUv = null;
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

        // Hook for subclasses that need extra conditions to paint (e.g. a loaded roller).
        protected virtual bool CanPaint() => true;

        void HandleContact(ToolContact contact)
        {
            if (!PaintableSurface.TryGet(contact.Collider, out PaintableSurface touched) || touched != surface)
            {
                LogWrongSurface(contact.Collider);
                return;
            }

            if (!CanPaint())
                return;

            tool.BeginUse(contact.Point);
            if (brush.IsRectangle)
                PaintRectangle(contact);
            else if (!PaintRound(contact))
                return;

            lastUvBySample[contact.SampleIndex] = brush.IsRectangle ? contact.Uv : paintedUv;
        }

        // Brush per probe sample: legacy UV size, or a real size in meters converted for this surface.
        // A tool resting in place paints nothing (returns false): stamping the same spot over and over would keep
        // whitening a faint stroke.
        bool PaintRound(ToolContact contact)
        {
            float scale = brush.UserScalable ? painter.BrushScale : 1f;
            bool continues = lastUvBySample.TryGetValue(contact.SampleIndex, out Vector2 lastUv);
            Vector2 uv = SmoothedUv(contact, continues);
            if (continues && Vector2.Distance(lastUv, uv) <= brush.MinStrokeDistance)
                return false;
            float white = brush.Directional ? StrokeWhite(contact) : 1f;

            if (!brush.InMeters)
            {
                if (continues)
                    painter.Line(mask, brush, lastUv, uv, scale, white);
                else
                    painter.Stamp(mask, brush, uv, scale, white);
                paintedUv = uv;
                return true;
            }

            Vector2 uvSize = MetersToUv(brush.SizeMeters * scale);
            if (continues)
                painter.LineSized(mask, brush, lastUv, uv, uvSize, white);
            else
                painter.StampSized(mask, brush, uv, uvSize, 0f, white);
            paintedUv = uv;
            return true;
        }

        // Low-pass filter on the tip path (per probe sample), so hand tremor doesn't kink the stroke at every step.
        Vector2 SmoothedUv(ToolContact contact, bool continues)
        {
            int sample = contact.SampleIndex;
            Vector2 smoothed = continues && smoothedUvBySample.TryGetValue(sample, out Vector2 previous)
                ? Vector2.Lerp(previous, contact.Uv, 1f - brush.PathSmoothing)
                : contact.Uv;
            smoothedUvBySample[sample] = smoothed;
            return smoothed;
        }

        // Directional brushes (the gouge): full white for strokes going up the surface, fainter sideways or downward.
        // "Up" is the slope of the surface away from the visitor (world up projected on it). A tool resting in place
        // keeps the white of its last stroke.
        float StrokeWhite(ToolContact contact)
        {
            int sample = contact.SampleIndex;
            // The first touch has no direction yet: it marks faint until the stroke shows where it goes.
            float white = lastWhiteBySample.TryGetValue(sample, out float last) ? last : brush.WhiteForAngle(90f);
            if (lastPointBySample.TryGetValue(sample, out Vector3 lastPoint))
            {
                Vector3 motion = Vector3.ProjectOnPlane(contact.Point - lastPoint, contact.Normal);
                if (motion.sqrMagnitude > 1e-8f)
                    white = brush.WhiteForAngle(Vector3.Angle(motion, SurfaceUp(contact.Normal)));
            }
            lastPointBySample[sample] = contact.Point;
            lastWhiteBySample[sample] = white;
            return white;
        }

        static Vector3 SurfaceUp(Vector3 normal)
        {
            Vector3 up = Vector3.ProjectOnPlane(Vector3.up, normal);
            if (up.sqrMagnitude < 1e-4f && Camera.main != null)
                up = Vector3.ProjectOnPlane(Camera.main.transform.forward, normal);
            return up.normalized;
        }

        // One rectangle for the whole tool (e.g. the roller band), aligned with its axle and swept from the last
        // painted point, using only the first touching sample of each physics step.
        void PaintRectangle(ToolContact contact)
        {
            if (Mathf.Approximately(lastRectangleTime, Time.fixedTime))
                return;
            lastRectangleTime = Time.fixedTime;

            Vector2 axis = surface.WorldToUvDirection(tool.transform.TransformDirection(brush.ToolAxis));
            if (axis.sqrMagnitude < 1e-10f)
                return;
            axis.Normalize();
            Vector2 across = new(-axis.y, axis.x);

            float length = brush.LengthMeters / MetersAlong(axis);
            float width = brush.SizeMeters / MetersAlong(across);
            float angle = Mathf.Atan2(axis.y, axis.x);
            Vector2 from = lastRectangleUv ?? contact.Uv;
            painter.Sweep(mask, brush, from, contact.Uv, new Vector2(length, width), angle, width * brush.StampSpacing);
            lastRectangleUv = contact.Uv;
        }

        Vector2 MetersToUv(float meters)
        {
            Vector2 perUv = surface.MetersPerUv;
            return new Vector2(meters / perUv.x, meters / perUv.y);
        }

        // World length (meters) of one UV unit along a normalized UV direction.
        float MetersAlong(Vector2 uvDirection)
        {
            Vector2 perUv = surface.MetersPerUv;
            return Mathf.Max(1e-5f, new Vector2(uvDirection.x * perUv.x, uvDirection.y * perUv.y).magnitude);
        }

        void HandleContactLost(int sampleIndex)
        {
            lastUvBySample.Remove(sampleIndex);
            lastPointBySample.Remove(sampleIndex);
            lastWhiteBySample.Remove(sampleIndex);
            smoothedUvBySample.Remove(sampleIndex);
            if (lastUvBySample.Count > 0)
                return;
            lastRectangleUv = null;
            tool.EndUse();
        }

        void HandleCoverage(float coverage)
        {
            ReportProgress(coverage);
            ReportCanAdvance(coverage >= Definition.MinCoverage);

            int decile = Mathf.FloorToInt(coverage * 10f);
            if (decile != lastLoggedDecile)
            {
                lastLoggedDecile = decile;
                XiloLog.Info(LogCategory, $"Coverage {Id}: {coverage:P0} (min {Definition.MinCoverage:P0})", this);
            }
        }

        void LogWrongSurface(Collider collider)
        {
            if (Time.time - lastWrongSurfaceLog < 1f)
                return;
            lastWrongSurfaceLog = Time.time;
            XiloLog.Info(LogCategory, $"{Id}: {collider.name} is not the step surface ({Definition.Surface})", this);
        }

        // Finds the step tool, its probe, the step surface and the mask; disables the step on failure.
        bool ResolveReferences()
        {
            if (tools == null || workbench == null || painter == null || coverageMeter == null || brush == null)
            {
                XiloLog.Error(LogCategory, $"{Id}: missing references", this);
                return false;
            }

            if (!tools.TryGet(Definition.ToolId, out tool) || !tool.TryGetComponent(out probe))
            {
                XiloLog.Error(LogCategory, $"{Id}: tool {Definition.ToolId} or its probe not found", this);
                return false;
            }

            surface = workbench.Get(Definition.Surface);
            mask = ResolveMask();
            if (surface == null || mask == null)
            {
                XiloLog.Error(LogCategory, $"{Id}: surface {Definition.Surface} or mask {Definition.MaskProperty} not found", this);
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
