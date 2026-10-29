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
        const float WeldPrecision = 1e5f;

        public static void WriteToUV(Mesh mesh)
        {
            mesh.SetUVs(Channel, Compute(mesh));
        }

        public static List<Vector3> Compute(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            var sums = new Dictionary<Vector3Int, Vector3>();

            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                int[] triangles = mesh.GetTriangles(subMesh);
                for (int i = 0; i + 2 < triangles.Length; i += 3)
                    AddTriangle(vertices, triangles[i], triangles[i + 1], triangles[i + 2], sums);
            }

            Vector3[] fallback = mesh.normals;
            var smooth = new List<Vector3>(vertices.Length);
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 sum = sums.TryGetValue(Key(vertices[i]), out Vector3 value) ? value : Vector3.zero;
                if (sum.sqrMagnitude < 1e-12f && fallback.Length == vertices.Length)
                    sum = fallback[i];
                smooth.Add(sum.normalized);
            }
            return smooth;
        }

        static void AddTriangle(Vector3[] vertices, int a, int b, int c, Dictionary<Vector3Int, Vector3> sums)
        {
            Vector3 pa = vertices[a], pb = vertices[b], pc = vertices[c];
            Vector3 face = Vector3.Cross(pb - pa, pc - pa);
            if (face.sqrMagnitude < 1e-20f)
                return;
            face.Normalize();

            Accumulate(sums, pa, face * Vector3.Angle(pb - pa, pc - pa));
            Accumulate(sums, pb, face * Vector3.Angle(pc - pb, pa - pb));
            Accumulate(sums, pc, face * Vector3.Angle(pa - pc, pb - pc));
        }

        static void Accumulate(Dictionary<Vector3Int, Vector3> sums, Vector3 position, Vector3 weighted)
        {
            Vector3Int key = Key(position);
            sums.TryGetValue(key, out Vector3 sum);
            sums[key] = sum + weighted;
        }

        // Positions are welded on a fine grid so float noise between split vertices doesn't separate them.
        static Vector3Int Key(Vector3 position)
        {
            return Vector3Int.RoundToInt(position * WeldPrecision);
        }
    }
}
