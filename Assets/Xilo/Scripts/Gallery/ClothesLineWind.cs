using System.Collections.Generic;
using UnityEngine;

namespace Xilo.Gallery
{
    // Fake wind blowing along the lines. Each sheet is a damped spring pushed by travelling gusts sampled at its
    // position: after bending one way it swings back past rest with a smaller amplitude (keeps its momentum).
    // The bend drives the "Front"/"Back" blend shapes; one grows while the other shrinks.
    public sealed class ClothesLineWind : MonoBehaviour
    {
        [SerializeField] List<SkinnedMeshRenderer> sheets = new();
        [SerializeField] string frontShape = "Front";
        [SerializeField] string backShape = "Back";

        [Header("Wind")]
        [Tooltip("World direction the gusts travel (left → right for the visitor).")]
        [SerializeField] Vector3 direction = Vector3.right;
        [Tooltip("How fast the gusts travel along the lines (m/s).")]
        [SerializeField] float speed = 0.6f;
        [Tooltip("Distance between two gusts (m). Shorter = neighbouring sheets move more differently.")]
        [SerializeField] float wavelength = 1.2f;
        [Tooltip("How hard a gust pushes (1 ≈ full bend at the gust peak).")]
        [SerializeField, Range(0f, 2f)] float push = 0.8f;
        [Tooltip("Slow variation of the gust strength (0 = constant).")]
        [SerializeField, Range(0f, 1f)] float gustiness = 0.6f;
        [SerializeField] float gustFrequency = 0.15f;
        [Tooltip("Irregular timing between gusts plus occasional calm spells, so it never looks like a loop (0 = regular).")]
        [SerializeField, Range(0f, 1f)] float randomness = 0.6f;
        [Tooltip("Random seed: change it for a different (still natural) wind pattern.")]
        [SerializeField] float seed = 17.3f;

        [Header("Sheet swing")]
        [Tooltip("Swings per second of a sheet (its natural frequency).")]
        [SerializeField, Range(0.2f, 4f)] float swingFrequency = 1.1f;
        [Tooltip("How fast the swing dies out. Low = several smaller swings back and forth; 1 = no overshoot.")]
        [SerializeField, Range(0.02f, 1f)] float damping = 0.2f;
        [Tooltip("Peak blend shape weight (0..100).")]
        [SerializeField, Range(0f, 100f)] float strength = 70f;

        const int SubSteps = 4;

        int[] frontIndex;
        int[] backIndex;
        float[] bend;
        float[] velocity;

        void Awake()
        {
            int count = sheets.Count;
            frontIndex = new int[count];
            backIndex = new int[count];
            bend = new float[count];
            velocity = new float[count];
            for (int i = 0; i < count; i++)
            {
                Mesh mesh = sheets[i] != null ? sheets[i].sharedMesh : null;
                frontIndex[i] = mesh != null ? mesh.GetBlendShapeIndex(frontShape) : -1;
                backIndex[i] = mesh != null ? mesh.GetBlendShapeIndex(backShape) : -1;
            }
        }

        void Update()
        {
            Vector3 axis = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.right;
            float omega = swingFrequency * Mathf.PI * 2f;
            float dt = Time.deltaTime / SubSteps;

            for (int i = 0; i < sheets.Count; i++)
            {
                if (sheets[i] == null)
                    continue;

                float along = Vector3.Dot(sheets[i].transform.position, axis);
                float target = Gust(along, Time.time) * push;
                for (int step = 0; step < SubSteps; step++)
                    Integrate(i, target, omega, dt);

                float value = Mathf.Clamp(bend[i], -1f, 1f);
                SetWeight(sheets[i], frontIndex[i], Mathf.Max(0f, value) * strength);
                SetWeight(sheets[i], backIndex[i], Mathf.Max(0f, -value) * strength);
            }
        }

        // Damped spring towards the wind target (semi-implicit Euler, stable with the sub-steps).
        void Integrate(int i, float target, float omega, float dt)
        {
            float acceleration = omega * omega * (target - bend[i]) - 2f * damping * omega * velocity[i];
            velocity[i] += acceleration * dt;
            bend[i] += velocity[i] * dt;
        }

        // 0..1 travelling pulses (the wind pushes one way); the spring makes the sheet swing back on its own.
        // Everything is sampled at the gust's arrival time at this position, so randomness still travels along the line.
        float Gust(float along, float time)
        {
            float arrival = time - along / Mathf.Max(0.01f, speed);
            float jitter = (Mathf.PerlinNoise(arrival * 0.23f, seed) - 0.5f) * 4f * Mathf.PI * randomness;
            float phase = arrival * speed / Mathf.Max(0.01f, wavelength) * Mathf.PI * 2f + jitter;
            float pulse = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(phase)), 3f);

            float strengthVariation = Mathf.Lerp(1f, Mathf.PerlinNoise(arrival * gustFrequency, seed + 0.37f) * 1.6f, gustiness);
            float calm = Mathf.Lerp(1f, Mathf.Clamp01((Mathf.PerlinNoise(arrival * 0.11f, seed + 5.3f) - 0.3f) * 3f), randomness);
            return pulse * strengthVariation * calm;
        }

        static void SetWeight(SkinnedMeshRenderer sheet, int index, float weight)
        {
            if (index >= 0)
                sheet.SetBlendShapeWeight(index, weight);
        }
    }
}
