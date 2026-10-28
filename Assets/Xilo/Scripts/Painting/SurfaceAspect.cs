using UnityEngine;

namespace Xilo.Painting
{
    // Measures how a face maps its UVs into the world: world length of one full UV unit along U and along V
    // (from the first triangle with non-degenerate UVs). Works whether the UVs span 0..1 or only part of it.
    public static class SurfaceAspect
    {
        // aspect = world length per V unit / world length per U unit; size = (per U, per V) in meters.
        public static bool TryMeasure(MeshFilter filter, out float aspect, out Vector2 size)
        {
            aspect = 1f;
            size = Vector2.zero;
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (mesh == null)
                return false;

            Vector3[] vertices = mesh.vertices;
            Vector2[] uvs = mesh.uv;
            int[] triangles = mesh.triangles;
            if (uvs.Length != vertices.Length)
                return false;

            Vector3 scale = filter.transform.lossyScale;
            for (int t = 0; t < triangles.Length; t += 3)
            {
                Vector3 p0 = Vector3.Scale(vertices[triangles[t]], scale);
                Vector3 e1 = Vector3.Scale(vertices[triangles[t + 1]], scale) - p0;
                Vector3 e2 = Vector3.Scale(vertices[triangles[t + 2]], scale) - p0;
                Vector2 d1 = uvs[triangles[t + 1]] - uvs[triangles[t]];
                Vector2 d2 = uvs[triangles[t + 2]] - uvs[triangles[t]];
                float determinant = d1.x * d2.y - d2.x * d1.y;
                if (Mathf.Abs(determinant) < 1e-6f)
                    continue;

                // Position derivatives along U and V (the classic tangent/bitangent solve).
                Vector3 perU = (e1 * d2.y - e2 * d1.y) / determinant;
                Vector3 perV = (e2 * d1.x - e1 * d2.x) / determinant;
                size = new Vector2(perU.magnitude, perV.magnitude);
                if (size.x <= 0f)
                    return false;
                aspect = size.y / size.x;
                return true;
            }
            return false;
        }
    }
}
