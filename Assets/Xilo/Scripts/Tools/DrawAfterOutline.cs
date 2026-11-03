using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Xilo.Core;

namespace Xilo.Tools
{
    // Keeps objects visible in front of an outlined one (e.g. the controllers over the art folder). The outline hull
    // is drawn over everything (depth test Always), so whatever is drawn before it disappears where the hull covers
    // it: the renderers under this object get runtime copies of their materials queued right after the outline.
    public sealed class DrawAfterOutline : MonoBehaviour
    {
        const string LogCategory = "Outline";

        [SerializeField] Material outlineMaterial;

        readonly List<Material> raisedMaterials = new();

        void Start()
        {
            if (outlineMaterial == null)
            {
                XiloLog.Error(LogCategory, "Outline material not assigned", this);
                return;
            }

            Raise(GetComponentsInChildren<Renderer>(true), outlineMaterial.renderQueue + 1, raisedMaterials);
            XiloLog.Info(LogCategory, $"{raisedMaterials.Count} materials drawn after the outline", this);
        }

        void OnDestroy()
        {
            foreach (Material material in raisedMaterials)
                Destroy(material);
        }

        // Renderers with a material queued before `after` get material copies queued at `after` (assets untouched).
        // The copies are added to `copies` so the caller can destroy them.
        public static void Raise(IEnumerable<Renderer> renderers, int after, List<Material> copies)
        {
            foreach (Renderer target in renderers)
            {
                if (target.sharedMaterials.All(material => material == null || material.renderQueue >= after))
                    continue;

                Material[] materials = target.materials;
                foreach (Material material in materials)
                {
                    copies.Add(material);
                    if (material.renderQueue < after)
                        material.renderQueue = after;
                }
            }
        }
    }
}
