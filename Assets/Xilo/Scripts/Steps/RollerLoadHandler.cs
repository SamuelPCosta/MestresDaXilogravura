using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using Xilo.Core;
using Xilo.Objects;
using Xilo.Painting;
using Xilo.Tools;

namespace Xilo.Steps
{
    // Loading step: the roller gets ink when it touches an inked area of the glass.
    // The ink mask is read once on enter so each contact can be checked on the CPU.
    public sealed class RollerLoadHandler : StepHandlerBase
    {
        const string LogCategory = "RollerLoad";
        const int SampleResolution = 128;

        [SerializeField] ToolRegistry tools;
        [SerializeField] Workbench workbench;
        [SerializeField] RollerInk rollerInk;
        [SerializeField] string inkMaskProperty = ShaderProps.InkMask;

        XiloTool roller;
        ToolTipProbe probe;
        PaintableSurface glass;
        byte[] inkPixels;
        bool hasAnyInk;

        protected override void OnEnter(StepContext context)
        {
            if (!ResolveReferences())
                return;

            probe.Contact += HandleContact;
            rollerInk.Loaded += HandleLoaded;
            ReadInkMask();
            ReportProgress(rollerInk.IsLoaded ? 1f : 0f);
            ReportCanAdvance(rollerInk.IsLoaded);
        }

        protected override void OnExit()
        {
            if (probe != null)
                probe.Contact -= HandleContact;
            if (rollerInk != null)
                rollerInk.Loaded -= HandleLoaded;
            roller?.EndUse();
        }

        public override void ClearProgress()
        {
            rollerInk?.ResetInk();
            ReportProgress(0f);
            ReportCanAdvance(false);
        }

        public override void DebugFill()
        {
            rollerInk?.Load();
        }

        void HandleContact(ToolContact contact)
        {
            if (rollerInk.IsLoaded || rollerInk.IsLoading)
                return;
            if (!PaintableSurface.TryGet(contact.Collider, out PaintableSurface touched) || touched != glass)
                return;
            if (!IsInked(contact.Uv))
                return;

            roller.BeginUse(contact.Point);
            ReportProgress(0.5f);
            rollerInk.Load();
        }

        void HandleLoaded()
        {
            roller?.EndUse();
            ReportProgress(1f);
            ReportCanAdvance(true);
        }

        // Without a readback (or with an empty mask) any glass contact counts, so the user never gets stuck.
        bool IsInked(Vector2 uv)
        {
            if (inkPixels == null || !hasAnyInk)
                return true;

            int x = Mathf.Clamp((int)(uv.x * SampleResolution), 0, SampleResolution - 1);
            int y = Mathf.Clamp((int)(uv.y * SampleResolution), 0, SampleResolution - 1);
            return inkPixels[(y * SampleResolution + x) * 4] >= CoverageCalculator.PaintedThreshold;
        }

        void ReadInkMask()
        {
            inkPixels = null;
            RenderTexture mask = glass.GetMask(inkMaskProperty);
            if (mask == null || !SystemInfo.supportsAsyncGPUReadback)
            {
                XiloLog.Warn(LogCategory, "Ink mask unavailable, any glass contact will load the roller", this);
                return;
            }

            var temp = RenderTexture.GetTemporary(SampleResolution, SampleResolution, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(mask, temp);
            AsyncGPUReadback.Request(temp, 0, TextureFormat.RGBA32, request =>
            {
                RenderTexture.ReleaseTemporary(temp);
                if (request.hasError)
                {
                    XiloLog.Warn(LogCategory, "Ink mask readback failed", this);
                    return;
                }

                NativeArray<byte> data = request.GetData<byte>();
                inkPixels = data.ToArray();
                hasAnyInk = CoverageCalculator.Compute(inkPixels, CoverageCalculator.BuildFullTarget(SampleResolution * SampleResolution)) > 0f;
                if (!hasAnyInk)
                    XiloLog.Warn(LogCategory, "No ink on the glass, any glass contact will load the roller", this);
            });
        }

        bool ResolveReferences()
        {
            if (tools == null || workbench == null || rollerInk == null)
            {
                XiloLog.Error(LogCategory, "Missing references", this);
                return false;
            }

            if (!tools.TryGet(Definition.ToolId, out roller) || !roller.TryGetComponent(out probe))
            {
                XiloLog.Error(LogCategory, $"Tool {Definition.ToolId} or its probe not found", this);
                return false;
            }

            glass = workbench.Get(Definition.Surface);
            if (glass == null)
            {
                XiloLog.Error(LogCategory, $"Surface {Definition.Surface} not found", this);
                return false;
            }
            return true;
        }
    }
}
