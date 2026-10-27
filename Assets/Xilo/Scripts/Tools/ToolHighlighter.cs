using System.Collections.Generic;
using UnityEngine;

namespace Xilo.Tools
{
    // Appends the existing outline material to the tool renderers while highlighted.
    public sealed class ToolHighlighter : MonoBehaviour
    {
        [SerializeField] Material outlineMaterial;
        [SerializeField] Renderer[] targetRenderers;

        bool isHighlighted;

        void Awake()
        {
            if (targetRenderers == null || targetRenderers.Length == 0)
                targetRenderers = GetComponentsInChildren<MeshRenderer>(true);
        }

        public void SetOutlineMaterial(Material material)
        {
            if (outlineMaterial == material)
                return;

            bool wasHighlighted = isHighlighted;
            SetHighlighted(false);
            outlineMaterial = material;
            SetHighlighted(wasHighlighted);
        }

        // Adds or removes the outline material on every target renderer.
        public void SetHighlighted(bool highlighted)
        {
            if (highlighted == isHighlighted || outlineMaterial == null)
                return;

            isHighlighted = highlighted;
            foreach (Renderer target in targetRenderers)
            {
                if (target == null)
                    continue;

                var materials = new List<Material>(target.sharedMaterials);
                if (highlighted)
                    materials.Add(outlineMaterial);
                else
                    materials.Remove(outlineMaterial);
                target.sharedMaterials = materials.ToArray();
            }
        }

        void OnDestroy()
        {
            SetHighlighted(false);
        }
    }
}
