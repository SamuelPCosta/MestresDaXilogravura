using System;
using UnityEngine;
using Xilo.Core;

namespace Xilo.Painting
{
    // Sizes the print on the sheet without touching the shader: the matrix masks and the art are copied into
    // sheet-space textures with a scale/offset, and the paper material reads those copies.
    public sealed class PaperPrintMapping : MonoBehaviour
    {
        const string LogCategory = "PrintMapping";

        [SerializeField] Workbench workbench;
        [SerializeField] PaintableSurface paper;

        [Header("PRINT SIZE ON THE SHEET")]
        [Tooltip("Size of the printed image on the sheet. 1 = fills the sheet UV; lower = smaller print. Tunable in Play mode.")]
        [SerializeField, Range(0.3f, 1.5f)] float printScale = 0.9f;
        [Tooltip("Moves the printed image on the sheet (sheet UV units).")]
        [SerializeField] Vector2 printOffset;
        [Tooltip("Reads the sheet height/width from its mesh so the print keeps the matrix proportions. Disable to use the value below.")]
        [SerializeField] bool autoSheetAspect = true;
        [Tooltip("World length per V unit / per U unit of the sheet UVs. The vertical print size is corrected by matrix aspect / sheet aspect.")]
        [SerializeField] float sheetAspect = 1f;

        [Header("Refresh")]
        [SerializeField] string[] matrixMasks = { ShaderProps.SketchMask, ShaderProps.SculptMask, ShaderProps.PaintMask };
        [SerializeField] float refreshInterval = 0.1f;

        PaintableSurface matrix;
        RenderTexture[] sources;
        RenderTexture[] mapped;
        RenderTexture mappedArt;
        float nextRefresh;

        void Start()
        {
            try
            {
                matrix = workbench != null ? workbench.Get(SurfaceId.Matrix) : null;
                if (matrix == null || paper == null)
                {
                    XiloLog.Error(LogCategory, "Workbench matrix or paper not assigned", this);
                    enabled = false;
                    return;
                }
                if (autoSheetAspect)
                    MeasureSheetAspect();
                CreateMappedMasks();
                Refresh();
            }
            catch (Exception exception)
            {
                XiloLog.Exception(LogCategory, exception, this);
                enabled = false;
            }
        }

        void OnDestroy()
        {
            if (mapped != null)
            {
                foreach (RenderTexture texture in mapped)
                    texture?.Release();
            }
            mappedArt?.Release();
        }

        void Update()
        {
            if (Time.time < nextRefresh)
                return;
            nextRefresh = Time.time + refreshInterval;
            Refresh();
        }

        // One sheet-space copy per matrix mask; the paper bindings are pointed at the copies.
        void CreateMappedMasks()
        {
            sources = new RenderTexture[matrixMasks.Length];
            mapped = new RenderTexture[matrixMasks.Length];
            for (int i = 0; i < matrixMasks.Length; i++)
            {
                sources[i] = matrix.GetMask(matrixMasks[i]);
                if (sources[i] == null)
                    continue;
                sources[i].wrapMode = TextureWrapMode.Clamp;
                mapped[i] = CreateLike(sources[i], $"Sheet_{matrixMasks[i]}");
                paper.ReplaceMask(matrixMasks[i], mapped[i]);
            }
            XiloLog.Info(LogCategory, $"Sheet-space masks created ({matrixMasks.Length})", this);
        }

        void MeasureSheetAspect()
        {
            if (SurfaceAspect.TryMeasure(paper.GetComponent<MeshFilter>(), out float aspect, out Vector2 size))
            {
                sheetAspect = aspect;
                XiloLog.Info(LogCategory, $"Sheet UV unit {size.x:0.###} x {size.y:0.###} m (aspect {aspect:0.###})", this);
            }
            else
                XiloLog.Warn(LogCategory, "Sheet UVs not measurable, keeping the configured aspect", this);
        }

        // Sheet UV p samples matrix UV m = (p - 0.5 - offset) / size + 0.5, where the print size keeps
        // the matrix proportions on the sheet: size.y = printScale * matrixAspect / sheetAspect.
        public void Refresh()
        {
            if (mapped == null)
                return;

            float vertical = sheetAspect > 0f ? workbench.MatrixAspect / sheetAspect : 1f;
            Vector2 size = new Vector2(printScale, printScale * vertical);
            Vector2 scale = new Vector2(1f / Mathf.Max(0.01f, size.x), 1f / Mathf.Max(0.01f, size.y));
            Vector2 offset = Vector2.one * 0.5f - Vector2.Scale(Vector2.one * 0.5f + printOffset, scale);

            // Sources are clamped and their borders are empty (masks) or white (art), so outside the image stays blank.
            for (int i = 0; i < mapped.Length; i++)
            {
                if (mapped[i] != null)
                    Graphics.Blit(sources[i], mapped[i], scale, offset);
            }

            if (workbench.CurrentArt != null)
            {
                mappedArt ??= CreateLike(workbench.CurrentArt, "Sheet_Art");
                Graphics.Blit(workbench.CurrentArt, mappedArt, scale, offset);
                paper.SetTexture(ShaderProps.SketchTexture, mappedArt);
            }
        }

        static RenderTexture CreateLike(Texture source, string textureName)
        {
            var texture = new RenderTexture(source.width, source.height, 0, RenderTextureFormat.ARGB32) { name = textureName, wrapMode = TextureWrapMode.Clamp };
            texture.Create();
            return texture;
        }
    }
}
