using System.Collections.Generic;
using UnityEngine;
using Xilo.Core;

namespace Xilo.Gallery
{
    // Fake wind blowing along the lines. Each sheet has two damped springs pushed by travelling gusts sampled at its
    // position, each driving a pair of opposite blend shapes (one grows while the other shrinks):
    // - bend ("Front"/"Back"): the gust pushes the sheet away; it swings back past rest with a smaller amplitude;
    // - curl ("Clockwise"/"Anticlockwise", seen from above): the edge the gust reaches first gives in before the
    //   other, and the edges trail behind the swing, so the sheet twists a little ahead of and behind each bend.
    // Each sheet swings at its own pace and a light flutter rides on top, so they read as thin paper, not boards.
    public sealed class ClothesLineWind : MonoBehaviour
    {
        [SerializeField] List<SkinnedMeshRenderer> sheets = new();
        [SerializeField] string frontShape = "Front";
        [SerializeField] string backShape = "Back";
        [SerializeField] string clockwiseShape = "Clockwise";
        [SerializeField] string anticlockwiseShape = "Anticlockwise";

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
        [Tooltip("Time (s) a sheet takes to give in to a gust and to let go of it. Higher = softer, lazier sheets; 0 = snaps to each gust.")]
        [SerializeField, Range(0f, 2f)] float windLag = 0.6f;
        [Tooltip("Each sheet swings at its own pace (± this share of Swing Frequency), so they don't move in unison like boards.")]
        [SerializeField, Range(0f, 0.5f)] float frequencySpread = 0.25f;

        [Header("Ease at the ends of a swing")]
        [Tooltip("How much the motion slows down around the end of each swing: the sheet reaches its furthest point, hangs there, " +
                 "starts back very slowly and then picks up speed (0 = plain spring, 0.9 = long hang).")]
        [SerializeField, Range(0f, 0.9f)] float peakHang = 0.75f;
        [Tooltip("Speed (as a share of a full swing per second) under which the motion counts as \"near the end\" and slows down. " +
                 "Higher = the ease covers more of the swing.")]
        [SerializeField, Range(0.05f, 1f)] float hangSpeed = 0.35f;

        [Header("Curl (seen from above)")]
        [Tooltip("Peak Clockwise/Anticlockwise blend shape weight (0..100).")]
        [SerializeField, Range(0f, 100f)] float curlStrength = 60f;
        [Tooltip("Sheet width along the line (m): the curl compares the wind at its two edges.")]
        [SerializeField, Min(0.01f)] float sheetWidth = 0.3f;
        [Tooltip("Curl from the gust reaching one edge before the other.")]
        [SerializeField, Range(0f, 5f)] float curlFromGustEdge = 2f;
        [Tooltip("Curl from the edges trailing behind the swing (the sheet twists while it moves, straightens at the ends).")]
        [SerializeField, Range(0f, 2f)] float curlFromSwing = 0.5f;
        [Tooltip("Curl swings per second, as a share of the sheet swing (different = the two motions drift in and out of step).")]
        [SerializeField, Range(0.3f, 2f)] float curlPace = 0.8f;
        [Tooltip("How fast the curl dies out (1 = no overshoot).")]
        [SerializeField, Range(0.02f, 1f)] float curlDamping = 0.35f;
        [Tooltip("Flip it if the sheets curl towards the wind instead of away from it.")]
        [SerializeField] bool invertCurl;

        [Header("Flutter (thin paper)")]
        [Tooltip("Quick irregular trembling on top of the swing, stronger while a gust blows (0 = none).")]
        [SerializeField, Range(0f, 0.5f)] float flutter = 0.03f;
        [Tooltip("Quick trembling of the curl (0 = none).")]
        [SerializeField, Range(0f, 0.5f)] float curlFlutter = 0.03f;
        [Tooltip("Trembles per second.")]
        [SerializeField, Range(0.5f, 12f)] float flutterFrequency = 1.2f;
        [Tooltip("Share of the flutter kept when there is no gust (0 = still sheets in calm air).")]
        [SerializeField, Range(0f, 1f)] float calmFlutter = 0.3f;

        const int SubSteps = 4;

        struct Spring
        {
            public float value;
            public float velocity;

            // Damped spring towards the target (semi-implicit Euler, stable with the sub-steps).
            public void Step(float target, float omega, float dampingRatio, float dt)
            {
                float acceleration = omega * omega * (target - value) - 2f * dampingRatio * omega * velocity;
                velocity += acceleration * dt;
                value += velocity * dt;
            }
        }

        sealed class SheetState
        {
            public int front = -1, back = -1, clockwise = -1, anticlockwise = -1;
            public Spring bend, curl;
            public float pace;
            public float feltGust, feltEdge;
        }

        SheetState[] states;

        void Awake()
        {
            states = new SheetState[sheets.Count];
            for (int i = 0; i < sheets.Count; i++)
                states[i] = CreateState(i);
        }

        SheetState CreateState(int i)
        {
            var state = new SheetState { pace = 1f + (Mathf.PerlinNoise(seed + i * 3.7f, 0.5f) * 2f - 1f) * frequencySpread };
            Mesh mesh = sheets[i] != null ? sheets[i].sharedMesh : null;
            if (mesh == null)
                return state;

            state.front = mesh.GetBlendShapeIndex(frontShape);
            state.back = mesh.GetBlendShapeIndex(backShape);
            state.clockwise = mesh.GetBlendShapeIndex(clockwiseShape);
            state.anticlockwise = mesh.GetBlendShapeIndex(anticlockwiseShape);
            if (state.clockwise < 0 || state.anticlockwise < 0)
                XiloLog.Warn("Wind", $"{sheets[i].name} has no {clockwiseShape}/{anticlockwiseShape} blend shapes: no curl", sheets[i]);
            return state;
        }

        void Update()
        {
            Vector3 axis = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.right;
            float omega = swingFrequency * Mathf.PI * 2f;

            for (int i = 0; i < sheets.Count; i++)
            {
                if (sheets[i] != null)
                    Simulate(sheets[i], states[i], i, Vector3.Dot(sheets[i].transform.position, axis), omega);
            }
        }

        // Feels the wind at the sheet centre and at its two edges, moves both springs and writes the blend shapes.
        void Simulate(SkinnedMeshRenderer sheet, SheetState state, int i, float along, float omega)
        {
            float time = Time.time;
            float halfWidth = sheetWidth * 0.5f;
            float gust = Feel(ref state.feltGust, Gust(along, time));
            float edge = Feel(ref state.feltEdge, Gust(along - halfWidth, time) - Gust(along + halfWidth, time));

            float bendOmega = omega * state.pace;
            float curlOmega = bendOmega * curlPace;
            float bendTarget = gust * push;
            float dt = Time.deltaTime / SubSteps;
            for (int step = 0; step < SubSteps; step++)
            {
                state.bend.Step(bendTarget, bendOmega, damping, dt * HangPace(state.bend.velocity, bendOmega));
                float trailing = -state.bend.velocity / bendOmega * curlFromSwing;
                state.curl.Step(edge * curlFromGustEdge + trailing, curlOmega, curlDamping, dt * HangPace(state.curl.velocity, curlOmega));
            }

            float gustShare = Mathf.Lerp(calmFlutter, 1f, Mathf.Clamp01(gust));
            float bendValue = state.bend.value + Flutter(i, 0f) * flutter * gustShare;
            float curlValue = (state.curl.value + Flutter(i, 7.9f) * curlFlutter * gustShare) * (invertCurl ? -1f : 1f);
            SetPair(sheet, state.front, state.back, bendValue, strength);
            SetPair(sheet, state.clockwise, state.anticlockwise, curlValue, curlStrength);
        }

        // Time runs slower for a spring while it moves slowly, i.e. around the end of a swing: it eases into its
        // furthest point, hangs, and starts back slowly before picking up speed. Fast parts of the swing are untouched.
        float HangPace(float velocity, float omega)
        {
            if (peakHang <= 0f)
                return 1f;
            float swingSpeed = Mathf.Abs(velocity) / omega;
            return Mathf.Lerp(1f - peakHang, 1f, Mathf.SmoothStep(0f, 1f, swingSpeed / hangSpeed));
        }

        // Thin paper doesn't snap to the wind: what it feels builds up and dies down over Wind Lag seconds.
        float Feel(ref float felt, float wind)
        {
            float blend = windLag > 0f ? 1f - Mathf.Exp(-Time.deltaTime / windLag) : 1f;
            felt = Mathf.Lerp(felt, wind, blend);
            return felt;
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

        // Fast two-octave noise (-1.5..1.5) added on top of a spring (not filtered by it); each sheet and each motion
        // (by offset) has its own track.
        float Flutter(int i, float offset)
        {
            float t = Time.time * flutterFrequency;
            float track = seed + i * 11.3f + offset;
            return (Mathf.PerlinNoise(t, track) - 0.5f) * 2f + (Mathf.PerlinNoise(t * 2.3f, track + 5.1f) - 0.5f);
        }

        // value > 0 drives the first shape, value < 0 the second one.
        static void SetPair(SkinnedMeshRenderer sheet, int positive, int negative, float value, float peak)
        {
            value = Mathf.Clamp(value, -1f, 1f);
            SetWeight(sheet, positive, Mathf.Max(0f, value) * peak);
            SetWeight(sheet, negative, Mathf.Max(0f, -value) * peak);
        }

        static void SetWeight(SkinnedMeshRenderer sheet, int index, float weight)
        {
            if (index >= 0)
                sheet.SetBlendShapeWeight(index, weight);
        }
    }
}
