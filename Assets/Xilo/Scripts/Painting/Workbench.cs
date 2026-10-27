using System;
using UnityEngine;
using Xilo.Core;

namespace Xilo.Painting
{
    // Groups the workbench surfaces and applies the chosen art to the matrix and the paper.
    public sealed class Workbench : MonoBehaviour
    {
        const string LogCategory = "Workbench";

        [SerializeField] PaintableSurface matrix;
        [SerializeField] PaintableSurface glass;
        [SerializeField] PaintableSurface paper;

        [Tooltip("Applied on Start until the art selection step is implemented.")]
        [SerializeField] Texture2D previewArt;

        public Texture2D CurrentArt { get; private set; }

        public event Action<Texture2D> ArtApplied;

        void Start()
        {
            if (previewArt != null)
                ApplyArt(previewArt);
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

            foreach (PaintableSurface surface in new[] { matrix, paper })
            {
                if (surface == null)
                    continue;
                surface.SetTexture(ShaderProps.SketchTexture, art);
                surface.SetFloat(ShaderProps.Authorial, 0f);
            }

            CurrentArt = art;
            XiloLog.Info(LogCategory, $"Art applied: {art.name}", this);
            ArtApplied?.Invoke(art);
        }
    }
}
