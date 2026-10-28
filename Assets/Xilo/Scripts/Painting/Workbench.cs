using System;
using UnityEngine;
using Xilo.Core;

namespace Xilo.Painting
{
    // Groups the workbench surfaces and applies the chosen art to the matrix and the paper.
    // The art is fitted to the matrix proportions (centered, white bars) so it is never stretched by the UVs.
    public sealed class Workbench : MonoBehaviour
    {
        const string LogCategory = "Workbench";

        [SerializeField] PaintableSurface matrix;
        [SerializeField] PaintableSurface glass;
        [SerializeField] PaintableSurface paper;

        [Tooltip("Applied on Start until the art selection step is implemented.")]
        [SerializeField] Texture2D previewArt;

        [Header("Art aspect")]
        [Tooltip("Reads the matrix UV proportion from its mesh. Disable to use the value below.")]
        [SerializeField] bool autoMatrixAspect = true;
        [Tooltip("World length per V unit / per U unit of the matrix face. The art keeps its own proportion inside it.")]
        [SerializeField] float matrixAspect = 1.26f;
        [SerializeField] int artResolution = 1024;

        RenderTexture fittedArt;

        // The art as the shaders and the coverage see it (fitted to the matrix proportions).
        public Texture CurrentArt => fittedArt;

        public event Action<Texture> ArtApplied;

        void Start()
        {
            if (autoMatrixAspect)
                matrixAspect = MeasureMatrixAspect();
            if (previewArt != null)
                ApplyArt(previewArt);
        }

        void OnDestroy()
        {
            if (fittedArt != null)
                fittedArt.Release();
        }

        public PaintableSurface Get(SurfaceId id) => id switch
        {
            SurfaceId.Matrix => matrix,
            SurfaceId.Glass => glass,
            SurfaceId.Paper => paper,
            _ => null
        };

        // Sets the art on the matrix and the paper, always in non-authorial mode.
        public void ApplyArt(Texture2D art)
        {
            if (art == null)
            {
                XiloLog.Warn(LogCategory, "ApplyArt called with null texture", this);
                return;
            }

            try
            {
                FitArt(art);
                foreach (PaintableSurface surface in new[] { matrix, paper })
                {
                    if (surface == null)
                        continue;
                    surface.SetTexture(ShaderProps.SketchTexture, fittedArt);
                    surface.SetFloat(ShaderProps.Authorial, 0f);
                }

                XiloLog.Info(LogCategory, $"Art applied: {art.name} (matrix aspect {matrixAspect:0.###})", this);
                ArtApplied?.Invoke(fittedArt);
            }
            catch (Exception exception)
            {
                XiloLog.Exception(LogCategory, exception, this);
            }
        }

        // Letterboxes the art into a texture with the matrix proportions: it fills one axis and is centered on the other.
        void FitArt(Texture2D art)
        {
            int height = Mathf.Max(1, Mathf.RoundToInt(artResolution * matrixAspect));
            if (fittedArt == null || fittedArt.height != height)
            {
                if (fittedArt != null)
                    fittedArt.Release();
                fittedArt = new RenderTexture(artResolution, height, 0, RenderTextureFormat.ARGB32) { name = "FittedArt", wrapMode = TextureWrapMode.Clamp };
                fittedArt.Create();
            }

            float artAspect = (float)art.height / art.width;
            Vector2 size = artAspect < matrixAspect
                ? new Vector2(1f, artAspect / matrixAspect)
                : new Vector2(matrixAspect / artAspect, 1f);
            Vector2 min = (Vector2.one - size) * 0.5f;

            // A clamped copy keeps the outside of the art filled with its (white) border instead of repeating it.
            RenderTexture clamped = RenderTexture.GetTemporary(art.width, art.height, 0, RenderTextureFormat.ARGB32);
            try
            {
                clamped.wrapMode = TextureWrapMode.Clamp;
                Graphics.Blit(art, clamped);
                Graphics.Blit(clamped, fittedArt, new Vector2(1f / size.x, 1f / size.y), new Vector2(-min.x / size.x, -min.y / size.y));
            }
            finally
            {
                RenderTexture.ReleaseTemporary(clamped);
            }
        }

        public float MatrixAspect => matrixAspect;

        float MeasureMatrixAspect()
        {
            MeshFilter filter = matrix != null ? matrix.GetComponent<MeshFilter>() : null;
            if (!SurfaceAspect.TryMeasure(filter, out float aspect, out Vector2 size))
            {
                XiloLog.Warn(LogCategory, "Matrix UVs not measurable, keeping the configured aspect", this);
                return matrixAspect;
            }
            XiloLog.Info(LogCategory, $"Matrix UV unit {size.x:0.###} x {size.y:0.###} m (aspect {aspect:0.###})", this);
            return aspect;
        }
    }
}
