using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Xilo.Core;

namespace Xilo.Painting
{
    // Measures mask coverage asynchronously on a downscaled copy (AsyncGPUReadback),
    // comparing it against a target built once from the chosen art.
    public sealed class CoverageMeter : MonoBehaviour
    {
        const string LogCategory = "Coverage";

        [SerializeField] Workbench workbench;
        [SerializeField] int resolution = 128;
        [SerializeField] float interval = 0.25f;

        [Header("Dynamic targets (matrix masks)")]
        [SerializeField] SurfaceId matrixSurface = SurfaceId.Matrix;
        [SerializeField] string carveMaskProperty = ShaderProps.SculptMask;
        [SerializeField] string inkMaskProperty = ShaderProps.PaintMask;

        readonly Dictionary<CoverageTarget, bool[]> targets = new();
        RenderTexture sampleTexture;
        RenderTexture trackedMask;
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

        public void Track(RenderTexture mask, CoverageTarget target, Action<float> callback)
        {
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
                float coverage = CoverageCalculator.Compute(data.AsReadOnlySpan(), target);
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

        static bool IsDynamic(CoverageTarget target) => target is CoverageTarget.Uncarved or CoverageTarget.Inked;

        // Reads the matrix masks once when a step starts tracking and builds its target:
        // Uncarved = not carved; Inked = inked by the roller and not carved (the printable area).
        void BuildDynamicTarget(CoverageTarget target)
        {
            targets.Remove(target);
            PaintableSurface matrix = workbench != null ? workbench.Get(matrixSurface) : null;
            RenderTexture carveMask = matrix != null ? matrix.GetMask(carveMaskProperty) : null;
            RenderTexture inkMask = matrix != null ? matrix.GetMask(inkMaskProperty) : null;
            if (carveMask == null || (target == CoverageTarget.Inked && inkMask == null))
            {
                XiloLog.Warn(LogCategory, $"Matrix masks for {target} not found, using full surface", this);
                targets[target] = targets[CoverageTarget.FullSurface];
                return;
            }

            int version = trackingVersion;
            ReadMask(carveMask, version, carve =>
            {
                if (target == CoverageTarget.Uncarved)
                {
                    StoreDynamicTarget(target, CoverageCalculator.BuildUnpaintedTarget(carve));
                    return;
                }
                ReadMask(inkMask, version, ink => StoreDynamicTarget(target, CoverageCalculator.BuildInkedTarget(ink, carve)));
            });
        }

        void StoreDynamicTarget(CoverageTarget target, bool[] values)
        {
            targets[target] = values;
            XiloLog.Info(LogCategory, $"{target} target built: {CountTrue(values) * 100f / values.Length:0.#}% of the surface", this);
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
        void BuildArtTargets(Texture2D art)
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
