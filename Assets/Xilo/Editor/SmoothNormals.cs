using System.Collections.Generic;
using UnityEngine;

namespace Xilo.EditorTools
{
    // Smoothed normals for inverted-hull outlines: every vertex sharing a position gets the same direction,
    // so hard edges (split vertices) don't open gaps and the result doesn't depend on the pivot.
    // Each triangle contributes its face normal weighted by the corner angle (no bias from how many times
    // a vertex was split for UVs or hard edges).
    static class SmoothNormals
    {
        public const int Channel = 3;

        // Positions are measured relative to the mesh size (as if it were 1 unit big): meshes exported in
        // centimeters have such tiny numbers that Unity's Normalize returns zero for their face normals.
        const float WeldCell = 1e-5f;

        // Below this fraction of the summed weights the face normals cancel out (both sides of a thin sheet meet at
        // the same position): the direction would be noise and the hull would spike, so the vertex normal is used.
        const float CancelledFraction = 0.05f;

        public static void WriteToUV(Mesh mesh)
        {
            mesh.SetUVs(Channel, Compute(mesh));
        }

        public static List<Vector3> Compute(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            float size = mesh.bounds.size.magnitude;
            if (size > 0f)
            {
                for (int i = 0; i < vertices.Length; i++)
                    vertices[i] /= size;
            }
            float cell = WeldCell;
            var sums = new Dictionary<Vector3Int, Vector4>();

            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                int[] triangles = mesh.GetTriangles(subMesh);
                for (int i = 0; i + 2 < triangles.Length; i += 3)
                    AddTriangle(vertices, triangles[i], triangles[i + 1], triangles[i + 2], cell, sums);
            }

            Vector3[] fallback = mesh.normals;
            var smooth = new List<Vector3>(vertices.Length);
            for (int i = 0; i < vertices.Length; i++)
            {
                sums.TryGetValue(Key(vertices[i], cell), out Vector4 value);
                Vector3 sum = value;
                bool cancelled = sum.magnitude <= value.w * CancelledFraction;
                if (cancelled && fallback.Length == vertices.Length)
                    sum = fallback[i];
                smooth.Add(sum.normalized);
            }
            return smooth;
        }

        // Accumulates the angle-weighted face normal (xyz) and the total weight (w) per welded position.
        static void AddTriangle(Vector3[] vertices, int a, int b, int c, float cell, Dictionary<Vector3Int, Vector4> sums)
        {
            Vector3 pa = vertices[a], pb = vertices[b], pc = vertices[c];
            Vector3 face = Vector3.Cross(pb - pa, pc - pa);
            float area = face.magnitude;
            if (area < 1e-20f)
                return;
            // Divided by hand: Vector3.Normalize returns zero below 1e-5, which small triangles easily are.
            face /= area;

            Accumulate(sums, Key(pa, cell), face, Vector3.Angle(pb - pa, pc - pa));
            Accumulate(sums, Key(pb, cell), face, Vector3.Angle(pc - pb, pa - pb));
            Accumulate(sums, Key(pc, cell), face, Vector3.Angle(pa - pc, pb - pc));
        }

        static void Accumulate(Dictionary<Vector3Int, Vector4> sums, Vector3Int key, Vector3 face, float weight)
        {
            sums.TryGetValue(key, out Vector4 sum);
            Vector3 weighted = face * weight;
            sums[key] = sum + new Vector4(weighted.x, weighted.y, weighted.z, weight);
        }

        // Positions are welded on a fine grid so float noise between split vertices doesn't separate them.
        static Vector3Int Key(Vector3 position, float cell)
        {
            return Vector3Int.RoundToInt(position / cell);
        }
    }
}
