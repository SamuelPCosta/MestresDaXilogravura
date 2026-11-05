using UnityEngine;
using Xilo.Core;

namespace Xilo.Painting
{
    // How a tool marks a mask. The size is either in UV units (legacy) or in meters, converted on each surface
    // so the mark matches the tool whatever the surface UV scale (meters at the WorkbenchScale reference scale).
    // A length makes it a rectangle along a tool axis (e.g. the roller), swept by close stamps instead of round strokes.
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

        [Tooltip("Strokes are plain bands with no stamp at each segment end: square ends, and no brighter joints when painting faint.")]
        [SerializeField] bool flatStroke;
        [Tooltip("Filters the hand tremor out of the stroke path (0 = raw, 0.9 = very smooth); fewer kinks between segments.")]
        [SerializeField, Range(0f, 0.95f)] float pathSmoothing;
        [Tooltip("Strokes are round stamps laid at a fixed spacing instead of bands: no seams or gaps where the stroke " +
                 "changes direction, round ends, and faint strokes keep their white where the stamps overlap.")]
        [SerializeField] bool stampedStroke;
        [Tooltip("Distance between stamps of a stamped stroke, as a fraction of the brush diameter.")]
        [SerializeField, Range(0.05f, 0.5f)] float strokeSpacing = 0.15f;
        [Tooltip("Square put at both ends of a stamped stroke so it starts and ends square (a gouge cut). Empty = round ends.")]
        [SerializeField] Texture2D capTexture;

        [Header("Refine (after each stroke)")]
        [Tooltip("When the tool leaves the surface, tiny uncarved spots are turned white.")]
        [SerializeField] bool refineSpecks;
        [Tooltip("Mask white (0..1) below which a pixel belongs to a spot.")]
        [SerializeField, Range(0.05f, 1f)] float speckThreshold = 0.75f;
        [Tooltip("Largest spot (pixels of a 1024 mask, scaled to the mask size) that is turned white.")]
        [SerializeField, Min(1)] int maxSpeckArea = 24;

        [Header("Level another mask (sandpaper)")]
        [Tooltip("Mask property of the same surface leveled where this brush painted, when the tool leaves the surface: " +
                 "each pixel there becomes fully white or black. Empty = none.")]
        [SerializeField] string levelsMask;
        [Tooltip("White (0..1) from which a leveled pixel becomes white; below it, black.")]
        [SerializeField, Range(0.05f, 0.95f)] float levelThreshold = 0.5f;

        [Header("Stroke direction (gouge)")]
        [Tooltip("Strokes going up the surface (away from the visitor) paint full white; any other direction paints Min White.")]
        [SerializeField] bool directional;
        [Tooltip("Strokes within this angle (degrees) of straight up paint full white.")]
        [SerializeField, Range(0f, 90f)] float fullWhiteAngle = 12f;
        [Tooltip("White painted by a stroke outside Full White Angle (diagonal, sideways or downward), 0..1; 0 = those strokes don't carve.")]
        [SerializeField, Range(0f, 1f)] float minWhite = 0.2f;
        [Tooltip("Mask white (0..1) from which a pixel counts as painted for the step progress.")]
        [SerializeField, Range(0.05f, 1f)] float countsAsPaintedFrom = 0.5f;

        public Texture2D StampTexture => stampTexture;
        public Texture2D LineTexture => lineTexture != null ? lineTexture : stampTexture;
        public float Size => size;
        public float MinStrokeDistance => minStrokeDistance;
        public float SizeMeters => WorkbenchScale.Meters(sizeMeters);
        public float LengthMeters => WorkbenchScale.Meters(lengthMeters);
        public Vector3 ToolAxis => toolAxis;
        public float StampSpacing => stampSpacing;
        public bool UserScalable => userScalable;
        public bool Directional => directional;
        public bool FlatStroke => flatStroke;
        public float PathSmoothing => pathSmoothing;
        public bool StampedStroke => stampedStroke;
        public float StrokeSpacing => strokeSpacing;
        public Texture2D CapTexture => capTexture;
        public bool RefineSpecks => refineSpecks;
        public byte SpeckThreshold => (byte)Mathf.Clamp(Mathf.RoundToInt(speckThreshold * 255f), 1, 255);
        public int MaxSpeckArea => maxSpeckArea;
        public string LevelsMask => levelsMask;
        public byte LevelThreshold => (byte)Mathf.Clamp(Mathf.RoundToInt(levelThreshold * 255f), 1, 255);
        public byte PaintedThreshold => (byte)Mathf.Clamp(Mathf.RoundToInt(countsAsPaintedFrom * 255f), 1, 255);

        // White (0..1) painted by a stroke making this angle (degrees) with the "up" direction of the surface.
        public float WhiteForAngle(float angle)
        {
            return angle <= fullWhiteAngle ? 1f : minWhite;
        }
        public bool InMeters => sizeMeters > 0f;
        public bool IsRectangle => sizeMeters > 0f && lengthMeters > 0f;
    }
}
