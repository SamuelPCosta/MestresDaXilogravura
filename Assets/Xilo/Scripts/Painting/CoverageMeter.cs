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
                NativeArray<byte> data = request.GetData<byte>();
                float coverage = CoverageCalculator.Compute(data.AsReadOnlySpan(), GetTarget(trackedTarget));
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

            XiloLog.Warn(LogCategory, $"Target {target} not ready (no art yet), using full surface", this);
            return targets[CoverageTarget.FullSurface];
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
