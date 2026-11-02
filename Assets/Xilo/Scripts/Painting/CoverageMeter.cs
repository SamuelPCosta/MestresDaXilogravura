using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Xilo.Core;

namespace Xilo.Painting
{
    // Measures mask coverage asynchronously on a downscaled copy (AsyncGPUReadback),
    // comparing it against a target built from the chosen art or from the masks of the previous steps.
    public sealed class CoverageMeter : MonoBehaviour
    {
        const string LogCategory = "Coverage";
        // Below this share of the surface a dynamic target is treated as empty.
        const float MinTargetFraction = 0.005f;

        [SerializeField] Workbench workbench;
        [SerializeField] int resolution = 128;
        [SerializeField] float interval = 0.25f;

        [Header("Dynamic targets (matrix masks)")]
        [SerializeField] SurfaceId matrixSurface = SurfaceId.Matrix;
        [SerializeField] string sketchMaskProperty = ShaderProps.SketchMask;
        [SerializeField] string carveMaskProperty = ShaderProps.SculptMask;
        [SerializeField] string inkMaskProperty = ShaderProps.PaintMask;
        [SerializeField, Tooltip("Inked is measured in the sheet space: this surface's carve/ink masks (mapped by PaperPrintMapping).")]
        SurfaceId printSurface = SurfaceId.Paper;
        [SerializeField, Tooltip("Places the art area on the sheet for the Inked target (the print is smaller than the sheet).")]
        PaperPrintMapping printMapping;

        readonly Dictionary<CoverageTarget, bool[]> targets = new();
        RenderTexture sampleTexture;
        RenderTexture trackedMask;
        byte trackedThreshold = CoverageCalculator.PaintedThreshold;
        CoverageTarget trackedTarget;
        Action<float> onCoverage;
        float nextSampleTime;
        bool requestPending;
        int trackingVersion;

        void Awake()
        {
            sampleTexture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32) { name = "CoverageSample" };
            sampleTexture.Create();
            targets[CoverageTarget.FullSurface] = CoverageCalculator.BuildFullTarget(resolution * resolution);
        }

        void OnEnable()
        {
            if (workbench != null)
                workbench.ArtApplied += BuildArtTargets;
        }

        void OnDisable()
        {
            if (workbench != null)
                workbench.ArtApplied -= BuildArtTargets;
        }

        void OnDestroy()
        {
            if (sampleTexture != null)
                sampleTexture.Release();
        }

        // paintedThreshold: mask value (0..255) from which a pixel counts as painted (faint strokes may count less).
        public void Track(RenderTexture mask, CoverageTarget target, Action<float> callback, byte paintedThreshold = CoverageCalculator.PaintedThreshold)
        {
            trackedThreshold = paintedThreshold;
            trackedMask = mask;
            trackedTarget = target;
            onCoverage = callback;
            nextSampleTime = 0f;
            trackingVersion++;

            if (IsDynamic(target))
                BuildDynamicTarget(target);
        }

        public void Stop()
        {
            trackedMask = null;
            onCoverage = null;
            trackingVersion++;
        }

        // Forces a new measurement on the next frame (e.g. right after clearing a mask).
        public void Refresh()
        {
            nextSampleTime = 0f;
        }

        void Update()
        {
            if (trackedMask == null || requestPending || Time.time < nextSampleTime)
                return;

            nextSampleTime = Time.time + interval;
            RequestSample(trackedMask, trackingVersion);
        }

        void RequestSample(RenderTexture mask, int version)
        {
            if (!SystemInfo.supportsAsyncGPUReadback)
            {
                XiloLog.Warn(LogCategory, "AsyncGPUReadback not supported, reporting full coverage", this);
                onCoverage?.Invoke(1f);
                return;
            }

            Graphics.Blit(mask, sampleTexture);
            requestPending = true;
            AsyncGPUReadback.Request(sampleTexture, 0, TextureFormat.RGBA32, request => HandleSample(request, version));
        }

        void HandleSample(AsyncGPUReadbackRequest request, int version)
        {
            requestPending = false;
            if (version != trackingVersion || onCoverage == null)
                return;

            if (request.hasError)
            {
                XiloLog.Warn(LogCategory, "Coverage readback failed", this);
                return;
            }

            try
            {
                bool[] target = GetTarget(trackedTarget);
                if (target == null)
                    return;

                NativeArray<byte> data = request.GetData<byte>();
                float coverage = CoverageCalculator.Compute(data.AsReadOnlySpan(), target, trackedThreshold);
                onCoverage.Invoke(coverage);
            }
            catch (Exception exception)
            {
                XiloLog.Exception(LogCategory, exception, this);
            }
        }

        bool[] GetTarget(CoverageTarget target)
        {
            if (targets.TryGetValue(target, out bool[] result))
                return result;

                // Dynamic targets are still being read back; skip this sample instead of reporting a wrong value.
            if (IsDynamic(target))
                return null;

            XiloLog.Warn(LogCategory, $"Target {target} not ready (no art yet), using full surface", this);
            return targets[CoverageTarget.FullSurface];
        }

        static bool IsDynamic(CoverageTarget target) => target is CoverageTarget.Uncarved or CoverageTarget.Inked or CoverageTarget.Unsketched;

        // Reads the masks left by the previous steps once when a step starts tracking and builds its target:
        // Unsketched = where the pencil didn't go inside the art rectangle (what the gouge removes);
        // Uncarved = the drawing strokes still in relief (what sanding and inking work on);
        // Inked = the drawing strokes inked on the relief (what prints, measured on the sheet).
        // The art background and the white bars around the art never count, carved or not.
        void BuildDynamicTarget(CoverageTarget target)
        {
            targets.Remove(target);
            bool onSheet = target == CoverageTarget.Inked;
            bool[] region = target == CoverageTarget.Unsketched ? ArtArea() : Drawing(onSheet);
            PaintableSurface source = workbench != null ? workbench.Get(onSheet ? printSurface : matrixSurface) : null;
            RenderTexture previousMask = source != null ? source.GetMask(target == CoverageTarget.Unsketched ? sketchMaskProperty : carveMaskProperty) : null;
            RenderTexture inkMask = source != null ? source.GetMask(inkMaskProperty) : null;
            if (previousMask == null || (onSheet && inkMask == null))
            {
                XiloLog.Warn(LogCategory, $"Masks for {target} not found, using the whole region", this);
                targets[target] = region;
                return;
            }

            int version = trackingVersion;
            ReadMask(previousMask, version, previous =>
            {
                bool[] untouched = CoverageCalculator.Intersect(CoverageCalculator.BuildUnpaintedTarget(previous), region);
                if (!onSheet)
                {
                    StoreDynamicTarget(target, untouched, region);
                    return;
                }
                ReadMask(inkMask, version, ink =>
                    StoreDynamicTarget(target, CoverageCalculator.Intersect(CoverageCalculator.BuildInkedTarget(ink, previous), region), untouched, region));
            });
        }

        // The art rectangle on the matrix at the meter resolution.
        bool[] ArtArea()
        {
            Rect rect = workbench != null ? workbench.ArtRect : new Rect(0f, 0f, 1f, 1f);
            return CoverageCalculator.BuildRectTarget(resolution, rect);
        }

        // The dark strokes of the art (what stays in relief and prints): on the matrix, or where the print lands on the sheet.
        bool[] Drawing(bool onSheet)
        {
            if (!targets.TryGetValue(CoverageTarget.ArtDark, out bool[] strokes))
            {
                XiloLog.Warn(LogCategory, "Art strokes not ready, using the art rectangle", this);
                strokes = ArtArea();
            }
            if (!onSheet)
                return strokes;
            if (printMapping == null)
            {
                XiloLog.Warn(LogCategory, "Print mapping not assigned, the strokes are measured as on the matrix", this);
                return strokes;
            }
            return CoverageCalculator.Remap(strokes, resolution, uv => printMapping.SheetToMatrix(uv));
        }

        // An empty target would count as already complete (nothing left to cover) and unlock the step at once,
        // e.g. pressing a sheet over a matrix that got no ink. Falls back to a bigger area instead.
        void StoreDynamicTarget(CoverageTarget target, bool[] values, params bool[][] fallbacks)
        {
            bool[] chosen = values;
            foreach (bool[] fallback in fallbacks.Append(targets[CoverageTarget.FullSurface]))
            {
                if (CountTrue(chosen) >= chosen.Length * MinTargetFraction)
                    break;
                chosen = fallback;
            }

            targets[target] = chosen;
            float percent = CountTrue(chosen) * 100f / chosen.Length;
            if (chosen == values)
                XiloLog.Info(LogCategory, $"{target} target built: {percent:0.#}% of the surface", this);
            else
                XiloLog.Warn(LogCategory, $"{target} target was (almost) empty, using a fallback area: {percent:0.#}% of the surface", this);
        }

        // Downscales a mask and reads it back; the callback gets a copy (null-safe, ignored if tracking changed).
        void ReadMask(RenderTexture mask, int version, Action<byte[]> onRead)
        {
            var temp = RenderTexture.GetTemporary(resolution, resolution, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(mask, temp);
            AsyncGPUReadback.Request(temp, 0, TextureFormat.RGBA32, request =>
            {
                RenderTexture.ReleaseTemporary(temp);
                if (version != trackingVersion)
                    return;
                if (request.hasError)
                {
                    XiloLog.Warn(LogCategory, $"Readback of {mask.name} failed, using full surface", this);
                    targets[trackedTarget] = targets[CoverageTarget.FullSurface];
                    return;
                }

                try
                {
                    onRead(request.GetData<byte>().ToArray());
                }
                catch (Exception exception)
                {
                    XiloLog.Exception(LogCategory, exception, this);
                }
            });
        }

        static int CountTrue(bool[] values)
        {
            int count = 0;
            foreach (bool value in values)
            {
                if (value)
                    count++;
            }
            return count;
        }

        // Reads the art once at the meter resolution and builds the dark/light targets.
        void BuildArtTargets(Texture art)
        {
            var temp = RenderTexture.GetTemporary(resolution, resolution, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(art, temp);
            AsyncGPUReadback.Request(temp, 0, TextureFormat.RGBA32, request =>
            {
                RenderTexture.ReleaseTemporary(temp);
                if (request.hasError)
                {
                    XiloLog.Warn(LogCategory, "Art readback failed", this);
                    return;
                }

                NativeArray<byte> data = request.GetData<byte>();
                targets[CoverageTarget.ArtDark] = CoverageCalculator.BuildTarget(data.AsReadOnlySpan(), CoverageTarget.ArtDark);
                targets[CoverageTarget.ArtLight] = CoverageCalculator.BuildTarget(data.AsReadOnlySpan(), CoverageTarget.ArtLight);
                XiloLog.Info(LogCategory, $"Coverage targets built from {art.name}", this);
            });
        }
    }
}
