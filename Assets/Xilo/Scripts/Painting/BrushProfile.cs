using UnityEngine;

namespace Xilo.Painting
{
    [CreateAssetMenu(menuName = "Xilo/Brush Profile", fileName = "Brush_")]
    public sealed class BrushProfile : ScriptableObject
    {
        [SerializeField] Texture2D stampTexture;
        [SerializeField] Texture2D lineTexture;
        [SerializeField, Range(0.001f, 0.5f)] float size = 0.03f;
        [SerializeField, Range(0f, 0.1f)] float minStrokeDistance = 0.002f;

        public Texture2D StampTexture => stampTexture;
        public Texture2D LineTexture => lineTexture != null ? lineTexture : stampTexture;
        public float Size => size;
        public float MinStrokeDistance => minStrokeDistance;
    }
}
