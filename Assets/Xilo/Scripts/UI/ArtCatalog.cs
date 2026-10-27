using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xilo.UI
{
    [Serializable]
    public sealed class ArtEntry
    {
        public string displayName;
        public Sprite sprite;

        public Texture2D Texture => sprite != null ? sprite.texture : null;
    }

    [CreateAssetMenu(menuName = "Xilo/Art Catalog", fileName = "ArtCatalog")]
    public sealed class ArtCatalog : ScriptableObject
    {
        [SerializeField] List<ArtEntry> arts = new();

        public IReadOnlyList<ArtEntry> Arts => arts;
        public int Count => arts.Count;
    }
}
