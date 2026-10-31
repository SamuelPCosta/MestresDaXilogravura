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

        const int FadeLevels = 16;

        readonly System.Collections.Generic.Dictionary<(Texture2D, int), Texture2D> fadedBrushes = new();
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
            foreach (Texture2D faded in fadedBrushes.Values)
                Destroy(faded);
        }

        // Multiplier chosen by the user (brush size slider); applied by callers that opt in.
        public float BrushScale { get; private set; } = 1f;

        public void SetBrushScale(float scale)
        {
            BrushScale = Mathf.Clamp(scale, brushScaleRange.x, brushScaleRange.y);
            XiloLog.Info(LogCategory, $"Brush scale: {BrushScale:0.##}x", this);
        }

        public void Stamp(RenderTexture mask, BrushProfile brush, Vector2 uv, float sizeScale = 1f, float white = 1f)
        {
            if (!CanDraw(mask, brush))
                return;

            float size = brush.Size * sizeScale;
            Draw(mask, Faded(brush.StampTexture, white), uv, new Vector2(size, size), 0f);
        }

        // Fills the gap between two UV samples with a rotated line plus a stamp at the end.
        public void Line(RenderTexture mask, BrushProfile brush, Vector2 fromUv, Vector2 toUv, float sizeScale = 1f, float white = 1f)
        {
            if (!CanDraw(mask, brush))
                return;

            float size = brush.Size * sizeScale;
            Vector2 direction = toUv - fromUv;
            float length = direction.magnitude;
            if (length > brush.MinStrokeDistance)
            {
                float angle = Mathf.Atan2(direction.y, direction.x);
                Draw(mask, Faded(brush.LineTexture, white), (fromUv + toUv) * 0.5f, new Vector2(length, size), angle);
            }

            if (!brush.FlatStroke)
                Draw(mask, Faded(brush.StampTexture, white), toUv, new Vector2(size, size), 0f);
        }

        public void StampSized(RenderTexture mask, BrushProfile brush, Vector2 uv, Vector2 uvSize, float angle, float white = 1f)
        {
            if (CanDraw(mask, brush))
                Draw(mask, Faded(brush.StampTexture, white), uv, uvSize, angle);
        }

        // Round stroke with an explicit UV size: a circle in meters becomes an ellipse in UV when the surface
        // UV unit is not square. The line width is the ellipse diameter across the stroke.
        public void LineSized(RenderTexture mask, BrushProfile brush, Vector2 fromUv, Vector2 toUv, Vector2 uvSize, float white = 1f)
        {
            if (!CanDraw(mask, brush))
                return;

            Vector2 direction = toUv - fromUv;
            float length = direction.magnitude;
            if (length > brush.MinStrokeDistance)
            {
                float angle = Mathf.Atan2(direction.y, direction.x);
                Vector2 across = new(-direction.y / length, direction.x / length);
                float width = 1f / Mathf.Sqrt(Square(across.x / uvSize.x) + Square(across.y / uvSize.y));
                Draw(mask, Faded(brush.LineTexture, white), (fromUv + toUv) * 0.5f, new Vector2(length, width), angle);
            }

            if (!brush.FlatStroke)
                Draw(mask, Faded(brush.StampTexture, white), toUv, uvSize, 0f);
        }

        // Rectangle stamps (x = along the angle) from one UV point to another, close enough to form a band.
        public void Sweep(RenderTexture mask, BrushProfile brush, Vector2 fromUv, Vector2 toUv, Vector2 uvSize, float angle, float spacingUv)
        {
            float distance = Vector2.Distance(fromUv, toUv);
            int steps = Mathf.Clamp(Mathf.CeilToInt(distance / Mathf.Max(1e-5f, spacingUv)), 1, maxStampsPerFrame);
            for (int i = 1; i <= steps; i++)
            {
                if (!CanDraw(mask, brush))
                    return;
                Draw(mask, brush.StampTexture, Vector2.Lerp(fromUv, toUv, i / (float)steps), uvSize, angle);
            }
        }

        static float Square(float value) => value * value;

        // The draw shader lerps the mask towards white by the brush alpha, so a fainter stroke is a brush with lower
        // alpha. Copies of the brush at FadeLevels alpha steps are made once (read back from the GPU, so the brush
        // texture doesn't need Read/Write) and reused.
        Texture Faded(Texture2D source, float white)
        {
            int level = Mathf.Clamp(Mathf.RoundToInt(white * FadeLevels), 1, FadeLevels);
            if (level == FadeLevels || source == null)
                return source;

            var key = (source, level);
            if (fadedBrushes.TryGetValue(key, out Texture2D cached))
                return cached;

            Texture2D faded = MakeFaded(source, level / (float)FadeLevels);
            fadedBrushes[key] = faded;
            return faded;
        }

        static Texture2D MakeFaded(Texture2D source, float alphaScale)
        {
            RenderTexture temp = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Graphics.Blit(source, temp);
                RenderTexture.active = temp;
                var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true)
                {
                    name = $"{source.name} x{alphaScale:0.##}",
                    wrapMode = TextureWrapMode.Clamp,
                };
                copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                Color32[] pixels = copy.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i].a = (byte)(pixels[i].a * alphaScale);
                copy.SetPixels32(pixels);
                copy.Apply(false, true);
                return copy;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(temp);
            }
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
