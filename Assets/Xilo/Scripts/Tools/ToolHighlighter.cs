using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Xilo.Tools
{
    // Shows the outline on the tool while highlighted, hidden while the tool is in the hand.
    // Each tool renderer gets a twin child renderer drawing the same mesh with the outline material on every
    // sub-mesh: appending the outline to the original renderer would only outline its last sub-mesh
    // (e.g. just the label of the ink pot).
    // The outline shader draws its hull over everything (depth test Always), so the object itself must be drawn after
    // it to cover the inside and leave only the rim: materials queued before the outline get a later-queued copy.
    public sealed class ToolHighlighter : MonoBehaviour
    {
        static readonly int SizeId = Shader.PropertyToID(Xilo.Core.ShaderProps.OutlineSize);

        [SerializeField] Material outlineMaterial;
        [SerializeField] Renderer[] targetRenderers;
        [Tooltip("Outline width (meters) for this object only. 0 = the outline material's width.")]
        [SerializeField, Min(0f)] float width;
        [Tooltip("Outline a box around each renderer's bounds instead of its mesh: flat meshes (sheets of paper) have no " +
                 "volume for the outline hull to show around.")]
        [SerializeField] bool boxOutline;
        [Tooltip("Minimum box thickness (meters) along each axis when Box Outline is on.")]
        [SerializeField, Min(0.0001f)] float minBoxThickness = 0.004f;

        readonly List<MeshRenderer> outlines = new();
        readonly List<Mesh> generatedMeshes = new();
        readonly List<Material> raisedMaterials = new();
        XiloTool tool;
        bool requested;

        void Awake()
        {
            if (targetRenderers == null || targetRenderers.Length == 0)
                targetRenderers = GetComponentsInChildren<MeshRenderer>(true);
            tool = GetComponent<XiloTool>();
            CreateOutlines();
            DrawTargetsAfterOutline();
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

        void OnDestroy()
        {
            foreach (Mesh mesh in generatedMeshes)
                Destroy(mesh);
            foreach (Material material in raisedMaterials)
                Destroy(material);
        }

        public void SetOutlineMaterial(Material material)
        {
            if (outlineMaterial == material)
                return;
            outlineMaterial = material;
            foreach (MeshRenderer outline in outlines)
                outline.sharedMaterials = OutlineMaterials(outline.GetComponent<MeshFilter>().sharedMesh);
            DrawTargetsAfterOutline();
            Refresh();
        }

        // Materials queued at or before the outline get a copy queued right after it (the asset is untouched). Every
        // renderer of the object counts, not only the outlined ones: a part drawn before the outline would be painted
        // over by it (e.g. a folder cover over its back cover).
        void DrawTargetsAfterOutline()
        {
            if (outlineMaterial == null)
                return;

            int after = outlineMaterial.renderQueue + 1;
            foreach (Renderer target in GetComponentsInChildren<Renderer>(true))
            {
                if (outlines.Contains(target as MeshRenderer) || target.sharedMaterials.All(material => material == null || material.renderQueue >= after))
                    continue;

                Material[] materials = target.materials;
                foreach (Material material in materials)
                {
                    raisedMaterials.Add(material);
                    if (material.renderQueue < after)
                        material.renderQueue = after;
                }
            }
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
                Mesh mesh = filter.sharedMesh;
                if (boxOutline)
                    mesh = BoundsBox(filter.sharedMesh.bounds, twin.transform);
                twin.AddComponent<MeshFilter>().sharedMesh = mesh;

                var outline = twin.AddComponent<MeshRenderer>();
                outline.sharedMaterials = OutlineMaterials(mesh);
                outline.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                outline.receiveShadows = false;
                outline.enabled = false;
                if (width > 0f)
                {
                    var block = new MaterialPropertyBlock();
                    block.SetFloat(SizeId, width);
                    outline.SetPropertyBlock(block);
                }
                outlines.Add(outline);
            }
        }

        // Box around the mesh bounds, built in meters with the twin unscaled (inverse of its parent's scale) so the
        // outline width is the same on every object. Its 8 corners are shared, so the normals (also in UV3, where the
        // outline shader reads smoothed normals) point diagonally out and the hull grows evenly on every side.
        Mesh BoundsBox(Bounds localBounds, Transform twin)
        {
            Vector3 parentScale = twin.parent.lossyScale;
            twin.localScale = new Vector3(1f / parentScale.x, 1f / parentScale.y, 1f / parentScale.z);
            twin.localPosition = localBounds.center;

            Vector3 size = Vector3.Scale(localBounds.size, parentScale);
            Vector3 half = new Vector3(
                Mathf.Max(Mathf.Abs(size.x), minBoxThickness),
                Mathf.Max(Mathf.Abs(size.y), minBoxThickness),
                Mathf.Max(Mathf.Abs(size.z), minBoxThickness)) * 0.5f;

            var vertices = new Vector3[8];
            var normals = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f);
                vertices[i] = Vector3.Scale(corner, half);
                normals[i] = corner.normalized;
            }

            var mesh = new Mesh { name = $"{twin.name}_Box", vertices = vertices, normals = normals };
            mesh.SetUVs(3, normals);
            mesh.triangles = new[]
            {
                0, 2, 3, 0, 3, 1,
                4, 5, 7, 4, 7, 6,
                0, 4, 6, 0, 6, 2,
                1, 3, 7, 1, 7, 5,
                0, 1, 5, 0, 5, 4,
                2, 6, 7, 2, 7, 3,
            };
            mesh.RecalculateBounds();
            generatedMeshes.Add(mesh);
            return mesh;
        }

        Material[] OutlineMaterials(Mesh mesh)
        {
            return Enumerable.Repeat(outlineMaterial, Mathf.Max(1, mesh.subMeshCount)).ToArray();
        }
    }
}
