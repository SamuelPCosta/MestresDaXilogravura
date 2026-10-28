using System;
using UnityEngine;
using Xilo.Core;

namespace Xilo.Painting
{
    // Paints brush stamps into mask RenderTextures using the existing Hidden/DrawSprite material.
    public sealed class MaskPainter : MonoBehaviour
    {
        const string LogCategory = "Painter";

        [SerializeField] Material drawSpriteMaterial;
        [SerializeField] int maxStampsPerFrame = 16;
        [SerializeField, Tooltip("Limits for the user-chosen brush size multiplier.")]
        Vector2 brushScaleRange = new(0.25f, 5f);

        Material material;
        int stampsThisFrame;
        int lastFrame = -1;

        void Awake()
        {
            if (drawSpriteMaterial == null)
            {
                XiloLog.Error(LogCategory, "Draw sprite material not assigned", this);
                enabled = false;
                return;
            }

            material = new Material(drawSpriteMaterial);
        }

        void OnDestroy()
        {
            if (material != null)
                Destroy(material);
        }

        // Multiplier chosen by the user (brush size slider); applied by callers that opt in.
        public float BrushScale { get; private set; } = 1f;

        public void SetBrushScale(float scale)
        {
            BrushScale = Mathf.Clamp(scale, brushScaleRange.x, brushScaleRange.y);
            XiloLog.Info(LogCategory, $"Brush scale: {BrushScale:0.##}x", this);
        }

        public void Stamp(RenderTexture mask, BrushProfile brush, Vector2 uv, float sizeScale = 1f)
        {
            if (!CanDraw(mask, brush))
                return;

            float size = brush.Size * sizeScale;
            Draw(mask, brush.StampTexture, uv, new Vector2(size, size), 0f);
        }

        // Fills the gap between two UV samples with a rotated line plus a stamp at the end.
        public void Line(RenderTexture mask, BrushProfile brush, Vector2 fromUv, Vector2 toUv, float sizeScale = 1f)
        {
            if (!CanDraw(mask, brush))
                return;

            float size = brush.Size * sizeScale;
            Vector2 direction = toUv - fromUv;
            float length = direction.magnitude;
            if (length > brush.MinStrokeDistance)
            {
                float angle = Mathf.Atan2(direction.y, direction.x);
                Draw(mask, brush.LineTexture, (fromUv + toUv) * 0.5f, new Vector2(length, size), angle);
            }

            Draw(mask, brush.StampTexture, toUv, new Vector2(size, size), 0f);
        }

        public void Clear(RenderTexture mask)
        {
            FillWith(mask, Color.clear);
        }

        public void Fill(RenderTexture mask)
        {
            FillWith(mask, Color.white);
        }

        static void FillWith(RenderTexture mask, Color color)
        {
            if (mask == null)
                return;

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = mask;
            GL.Clear(true, true, color);
            RenderTexture.active = previous;
        }

        bool CanDraw(RenderTexture mask, BrushProfile brush)
        {
            if (material == null || mask == null || brush == null || brush.StampTexture == null)
                return false;

            if (lastFrame != Time.frameCount)
            {
                lastFrame = Time.frameCount;
                stampsThisFrame = 0;
            }

            return stampsThisFrame < maxStampsPerFrame;
        }

        // Blits the mask through the brush material (mask → temp → mask), as the legacy painter did.
        void Draw(RenderTexture mask, Texture texture, Vector2 uv, Vector2 scale, float rotation)
        {
            RenderTexture temp = null;
            try
            {
                stampsThisFrame++;
                material.SetTexture(ShaderProps.BrushSpriteId, texture);
                material.SetVector(ShaderProps.BrushPositionId, uv);
                material.SetVector(ShaderProps.BrushScaleId, scale);
                material.SetFloat(ShaderProps.BrushRotationId, rotation);

                temp = RenderTexture.GetTemporary(mask.width, mask.height, 0, mask.format);
                Graphics.Blit(mask, temp);
                Graphics.Blit(temp, mask, material);
            }
            catch (Exception exception)
            {
                XiloLog.Exception(LogCategory, exception, this);
            }
            finally
            {
                if (temp != null)
                    RenderTexture.ReleaseTemporary(temp);
            }
        }
    }
}
