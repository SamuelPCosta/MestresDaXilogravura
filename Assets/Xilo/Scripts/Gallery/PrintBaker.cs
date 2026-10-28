using System;
using UnityEngine;
using UnityEngine.Rendering;
using Xilo.Core;
using Xilo.Painting;

namespace Xilo.Gallery
{
    // Bakes the sheet as it looks now (its runtime material) into a PNG, like the legacy ShaderBaker:
    // the sheet mesh is drawn flat in an orthographic pass, framed by its own bounds (no extra camera).
    public sealed class PrintBaker : MonoBehaviour
    {
        const string LogCategory = "PrintBaker";

        [SerializeField] PaintableSurface paper;
        [SerializeField, Tooltip("Output pixels per mesh unit (the sheet mesh is 0.8 x 1.0).")] int pixelsPerUnit = 2048;
        [SerializeField] Color backgroundColor = Color.white;

        // Hooked to the step panel's Save button.
        public void SaveCurrentPrint()
        {
            Texture2D image = null;
            try
            {
                image = Bake();
                GalleryStorage.Save(image);
            }
            catch (Exception exception)
            {
                XiloLog.Exception(LogCategory, exception, this);
            }
            finally
            {
                if (image != null)
                    Destroy(image);
            }
        }

        Texture2D Bake()
        {
            Mesh mesh = paper.GetComponent<MeshFilter>().sharedMesh;
            Bounds bounds = mesh.bounds;
            int width = Mathf.Max(1, Mathf.RoundToInt(bounds.size.x * pixelsPerUnit));
            int height = Mathf.Max(1, Mathf.RoundToInt(bounds.size.y * pixelsPerUnit));

            // The SRP shader reads the camera matrices, which hold whatever camera rendered last when this runs
            // mid-frame (a UI click). So an explicit orthographic view framing the mesh bounds is set every time.
            Matrix4x4 projection = Matrix4x4.Ortho(bounds.min.x, bounds.max.x, bounds.min.y, bounds.max.y, -10f, 10f);

            RenderTexture target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            using var commands = new CommandBuffer { name = "Xilo PrintBaker" };
            try
            {
                commands.SetRenderTarget(target);
                commands.ClearRenderTarget(true, true, backgroundColor);
                commands.SetViewProjectionMatrices(Matrix4x4.identity, projection);
                commands.DrawMesh(mesh, Matrix4x4.identity, paper.Material, 0, 0);
                Graphics.ExecuteCommandBuffer(commands);

                var image = new Texture2D(width, height, TextureFormat.RGB24, false);
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                XiloLog.Info(LogCategory, $"Print baked {width}x{height}", this);
                return image;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
            }
        }
    }
}
