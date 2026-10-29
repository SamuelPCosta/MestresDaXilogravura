using UnityEngine;

namespace Xilo.Painting
{
    // How a tool marks a mask. The size is either in UV units (legacy) or in meters, converted on each surface
    // so the mark matches the tool whatever the surface UV scale. A length makes it a rectangle along a tool axis
    // (e.g. the roller), swept by close stamps instead of round strokes.
    [CreateAssetMenu(menuName = "Xilo/Brush Profile", fileName = "Brush_")]
    public sealed class BrushProfile : ScriptableObject
    {
        [SerializeField] Texture2D stampTexture;
        [SerializeField] Texture2D lineTexture;
        [Tooltip("Size in UV units (used when Size Meters is 0).")]
        [SerializeField, Range(0.001f, 0.5f)] float size = 0.03f;
        [SerializeField, Range(0f, 0.1f)] float minStrokeDistance = 0.002f;

        [Header("Real size (meters)")]
        [Tooltip("Diameter (or rectangle width) in meters. 0 = use the UV size above.")]
        [SerializeField, Min(0f)] float sizeMeters;
        [Tooltip("Rectangle length in meters along the tool axis (roller). 0 = round brush.")]
        [SerializeField, Min(0f)] float lengthMeters;
        [Tooltip("Tool local axis the rectangle length follows (roller axle).")]
        [SerializeField] Vector3 toolAxis = Vector3.right;
        [Tooltip("Distance between rectangle stamps, as a fraction of its width (smaller = smoother band).")]
        [SerializeField, Range(0.1f, 1f)] float stampSpacing = 0.4f;
        [Tooltip("Follows the brush size slider. Off for tools with a fixed physical size (baren, roller).")]
        [SerializeField] bool userScalable = true;

        public Texture2D StampTexture => stampTexture;
        public Texture2D LineTexture => lineTexture != null ? lineTexture : stampTexture;
        public float Size => size;
        public float MinStrokeDistance => minStrokeDistance;
        public float SizeMeters => sizeMeters;
        public float LengthMeters => lengthMeters;
        public Vector3 ToolAxis => toolAxis;
        public float StampSpacing => stampSpacing;
        public bool UserScalable => userScalable;
        public bool InMeters => sizeMeters > 0f;
        public bool IsRectangle => sizeMeters > 0f && lengthMeters > 0f;
    }
}
