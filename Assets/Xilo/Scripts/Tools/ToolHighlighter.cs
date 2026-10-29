using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Xilo.Tools
{
    // Shows the outline on the tool while highlighted, hidden while the tool is in the hand.
    // Each tool renderer gets a twin child renderer drawing the same mesh with the outline material on every
    // sub-mesh: appending the outline to the original renderer would only outline its last sub-mesh
    // (e.g. just the label of the ink pot).
    public sealed class ToolHighlighter : MonoBehaviour
    {
        [SerializeField] Material outlineMaterial;
        [SerializeField] Renderer[] targetRenderers;

        readonly List<MeshRenderer> outlines = new();
        XiloTool tool;
        bool requested;

        void Awake()
        {
            if (targetRenderers == null || targetRenderers.Length == 0)
                targetRenderers = GetComponentsInChildren<MeshRenderer>(true);
            tool = GetComponent<XiloTool>();
            CreateOutlines();
        }

        void OnEnable()
        {
            if (tool == null)
                return;
            tool.Grabbed += HandleHoldChanged;
            tool.Released += HandleHoldChanged;
        }

        void OnDisable()
        {
            if (tool == null)
                return;
            tool.Grabbed -= HandleHoldChanged;
            tool.Released -= HandleHoldChanged;
        }

        public void SetOutlineMaterial(Material material)
        {
            if (outlineMaterial == material)
                return;
            outlineMaterial = material;
            foreach (MeshRenderer outline in outlines)
                outline.sharedMaterials = OutlineMaterials(outline.GetComponent<MeshFilter>().sharedMesh);
            Refresh();
        }

        // Asks for the outline (e.g. the tool of the current step); it only shows while the tool is not held.
        public void SetHighlighted(bool highlighted)
        {
            requested = highlighted;
            Refresh();
        }

        void HandleHoldChanged(XiloTool _)
        {
            Refresh();
        }

        void Refresh()
        {
            bool visible = requested && outlineMaterial != null && !(tool != null && tool.IsHeld);
            foreach (MeshRenderer outline in outlines)
                outline.enabled = visible;
        }

        // One hidden twin per mesh renderer, sharing its mesh and following it as a child.
        void CreateOutlines()
        {
            foreach (Renderer target in targetRenderers)
            {
                if (target == null || !target.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
                    continue;

                var twin = new GameObject($"{target.name}_Outline");
                twin.transform.SetParent(target.transform, false);
                twin.layer = target.gameObject.layer;
                twin.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;

                var outline = twin.AddComponent<MeshRenderer>();
                outline.sharedMaterials = OutlineMaterials(filter.sharedMesh);
                outline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                outline.receiveShadows = false;
                outline.enabled = false;
                outlines.Add(outline);
            }
        }

        Material[] OutlineMaterials(Mesh mesh)
        {
            return Enumerable.Repeat(outlineMaterial, Mathf.Max(1, mesh.subMeshCount)).ToArray();
        }
    }
}
