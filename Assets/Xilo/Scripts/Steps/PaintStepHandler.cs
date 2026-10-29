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
            coverageMeter.Track(mask, Definition.CoverageTarget, HandleCoverage);
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
            lastRectangleUv = null;
        }

        public override void ClearProgress()
        {
            RenderTexture target = mask != null ? mask : ResolveMask();
            if (target != null && painter != null)
                painter.Clear(target);
            lastUvBySample.Clear();
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

            if (brush.IsRectangle)
                PaintRectangle(contact);
            else
                PaintRound(contact);

            lastUvBySample[contact.SampleIndex] = contact.Uv;
            tool.BeginUse(contact.Point);
        }

        // Round brush per probe sample: legacy UV size, or a real size in meters converted for this surface.
        void PaintRound(ToolContact contact)
        {
            float scale = brush.UserScalable ? painter.BrushScale : 1f;
            bool continues = lastUvBySample.TryGetValue(contact.SampleIndex, out Vector2 lastUv);

            if (!brush.InMeters)
            {
                if (continues)
                    painter.Line(mask, brush, lastUv, contact.Uv, scale);
                else
                    painter.Stamp(mask, brush, contact.Uv, scale);
                return;
            }

            Vector2 uvSize = MetersToUv(brush.SizeMeters * scale);
            if (continues)
                painter.LineSized(mask, brush, lastUv, contact.Uv, uvSize);
            else
                painter.StampSized(mask, brush, contact.Uv, uvSize, 0f);
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
