using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Xilo.Core;
using Xilo.Tools;

namespace Xilo.Objects
{
    // A heavy hit on the desk: every listed object hops quickly along the desk normal and lands back where it was.
    // The hit spreads from the impact point: the closest object hops first, then the next one, and so on.
    // Each one gets a random height so the reaction doesn't look mechanical.
    public sealed class ImpactHop : MonoBehaviour
    {
        const string LogCategory = "ImpactHop";

        [Tooltip("Objects that jump on the impact. Add or remove freely.")]
        [SerializeField] List<Transform> items = new();
        [Tooltip("Average hop height (meters).")]
        [SerializeField] float height = 0.06f;
        [Tooltip("Random height per object: 0.5 = between 50% and 150% of the height.")]
        [SerializeField, Range(0f, 1f)] float heightRandomness = 0.5f;
        [Tooltip("Up and down time (seconds).")]
        [SerializeField] float duration = 0.18f;
        [Tooltip("Random variation of the duration per object (0 = all equal).")]
        [SerializeField, Range(0f, 1f)] float durationRandomness = 0.2f;
        [Tooltip("Delay between one object and the next closest one (seconds).")]
        [SerializeField] float propagationDelay = 0.025f;

        readonly Dictionary<Transform, Coroutine> running = new();
        readonly Dictionary<Transform, Vector3> restPositions = new();
        readonly Dictionary<Transform, RigidbodyInterpolation> interpolations = new();

        // Makes every listed object hop along the given world direction, closest to the origin first.
        public void Hit(Vector3 up, Vector3 origin)
        {
            Vector3 direction = up.sqrMagnitude > 0f ? up.normalized : Vector3.up;
            List<Transform> ordered = items
                .Where(item => item != null && item.gameObject.activeInHierarchy && !IsBusy(item))
                .OrderBy(item => (item.position - origin).sqrMagnitude)
                .ToList();

            for (int rank = 0; rank < ordered.Count; rank++)
            {
                Transform item = ordered[rank];
                if (running.TryGetValue(item, out Coroutine previous))
                    StopCoroutine(previous);
                else
                    Remember(item);
                running[item] = StartCoroutine(Hop(item, direction, rank * propagationDelay));
            }
            XiloLog.Info(LogCategory, $"Impact: {ordered.Count} objects hop, closest first", this);
        }

        // Rest pose and rigidbody interpolation are read only while the object is still, so a hit that
        // interrupts a hop never takes a mid-air pose (or the paused interpolation) as the original.
        void Remember(Transform item)
        {
            restPositions[item] = item.localPosition;
            if (item.TryGetComponent(out Rigidbody body))
                interpolations[item] = body.interpolation;
        }

        // Held tools and objects already being animated are left alone.
        static bool IsBusy(Transform item)
        {
            if (item.TryGetComponent(out XiloTool tool) && tool.IsHeld)
                return true;
            return item.TryGetComponent(out PoseTween tween) && tween.IsMoving;
        }

        IEnumerator Hop(Transform item, Vector3 up, float delay)
        {
            Vector3 rest = restPositions[item];
            float jumpHeight = height * (1f + Random.Range(-heightRandomness, heightRandomness));
            float jumpDuration = Mathf.Max(0.01f, duration * (1f + Random.Range(-durationRandomness, durationRandomness)));
            Vector3 localUp = item.parent != null ? item.parent.InverseTransformVector(up) : up;

            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            // An interpolated rigidbody keeps writing its own (older) pose over the transform, so the object
            // would land slightly off and drift a bit higher on every hit. Interpolation is paused during the hop.
            item.TryGetComponent(out Rigidbody body);
            if (body != null)
                body.interpolation = RigidbodyInterpolation.None;

            for (float elapsed = 0f; elapsed < jumpDuration; elapsed += Time.deltaTime)
            {
                if (IsBusy(item))
                {
                    Finish(item, body);
                    yield break;
                }
                float t = elapsed / jumpDuration;
                item.localPosition = rest + localUp * (jumpHeight * 4f * t * (1f - t));
                yield return null;
            }

            item.localPosition = rest;
            if (body != null)
                body.position = item.position;
            Finish(item, body);
        }

        void Finish(Transform item, Rigidbody body)
        {
            if (body != null && interpolations.TryGetValue(item, out RigidbodyInterpolation interpolation))
                body.interpolation = interpolation;
            running.Remove(item);
            restPositions.Remove(item);
            interpolations.Remove(item);
        }
    }
}
