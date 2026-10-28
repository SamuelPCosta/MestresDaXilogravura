using System;
using System.Collections;
using UnityEngine;
using Xilo.Core;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Xilo.Objects
{
    // Drives this object through simple animated poses (move, spin, bump) with IEnumerator + Lerp/Slerp,
    // an ease-in/ease-out curve and an optional parabolic arc. Motions can be previewed outside Play mode.
    public sealed class PoseTween : MonoBehaviour
    {
        const string LogCategory = "Tween";

        [SerializeField] float defaultDuration = 1.5f;
        [SerializeField, Tooltip("Normalized time → normalized progress. Default is ease-in/ease-out.")]
        AnimationCurve easing = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField, Tooltip("Peak height (world up, meters) of the parabolic arc of MoveTo, reached halfway. 0 = straight line.")]
        float arcHeight;

        [Header("Editor preview")]
        [SerializeField, Tooltip("Target used by the context menu preview (right-click the component header).")]
        Transform previewTarget;
        [SerializeField, Tooltip("Pause at the end pose before returning in the previews.")]
        float previewHold = 0.5f;

        Coroutine running;

        public bool IsMoving => running != null;
        public float DefaultDuration => defaultDuration;

        public void MoveTo(Transform target, Action onCompleted = null)
        {
            MoveTo(target.position, target.rotation, defaultDuration, onCompleted);
        }

        public void MoveTo(Vector3 position, Quaternion rotation, float duration, Action onCompleted = null)
        {
            Play(MovePose(position, rotation), duration, onCompleted);
        }

        // Rotates in place around a world axis while lifting by a parabola that peaks at 'lift' halfway.
        public void SpinAround(Vector3 axis, float degrees, Vector3 lift, float duration, Action onCompleted = null)
        {
            Play(SpinPose(axis, degrees, lift), duration, onCompleted);
        }

        // Goes up by 'offset' and comes back to the start pose (e.g. a tool making room for the sheet).
        public void Bump(Vector3 offset, float duration, Action onCompleted = null)
        {
            Play(BumpPose(offset), duration, onCompleted);
        }

        public void Snap(Vector3 position, Quaternion rotation)
        {
            Stop();
            transform.SetPositionAndRotation(position, rotation);
        }

        public void Stop()
        {
            if (running == null)
                return;
            StopCoroutine(running);
            running = null;
        }

        public Func<float, (Vector3, Quaternion)> MovePose(Vector3 position, Quaternion rotation)
        {
            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            return t => (Vector3.LerpUnclamped(startPosition, position, t) + Vector3.up * (arcHeight * Parabola(t)),
                Quaternion.SlerpUnclamped(startRotation, rotation, t));
        }

        public Func<float, (Vector3, Quaternion)> SpinPose(Vector3 axis, float degrees, Vector3 lift)
        {
            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            return t => (startPosition + lift * Parabola(t), Quaternion.AngleAxis(degrees * t, axis) * startRotation);
        }

        public Func<float, (Vector3, Quaternion)> BumpPose(Vector3 offset)
        {
            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            return t => (startPosition + offset * Parabola(t), startRotation);
        }

        // 0 at both ends, 1 halfway.
        static float Parabola(float t) => 4f * t * (1f - t);

        public void Play(Func<float, (Vector3, Quaternion)> poseAt, float duration, Action onCompleted = null)
        {
            Stop();
            running = StartCoroutine(Animate(poseAt, Mathf.Max(0.01f, duration), onCompleted));
        }

        // Applies the eased pose every frame, then invokes the completion callback.
        IEnumerator Animate(Func<float, (Vector3 position, Quaternion rotation)> poseAt, float duration, Action onCompleted)
        {
            XiloLog.Info(LogCategory, $"{name}: animating over {duration:0.##}s", this);

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                var (position, rotation) = poseAt(easing.Evaluate(elapsed / duration));
                transform.SetPositionAndRotation(position, rotation);
                yield return null;
            }

            var (endPosition, endRotation) = poseAt(1f);
            transform.SetPositionAndRotation(endPosition, endRotation);
            running = null;

            try
            {
                onCompleted?.Invoke();
            }
            catch (Exception exception)
            {
                XiloLog.Exception(LogCategory, exception, this);
            }
        }

#if UNITY_EDITOR
        Func<float, (Vector3 position, Quaternion rotation)> previewPose;
        double previewStart;
        float previewDuration;
        Vector3 previewFromPosition;
        Quaternion previewFromRotation;
        bool previewReturns;

        [ContextMenu("Preview → target")]
        public void PreviewToTarget() => PreviewMoveToTarget(false);

        [ContextMenu("Preview → target and back")]
        public void PreviewToTargetAndBack() => PreviewMoveToTarget(true);

        [ContextMenu("Preview → stop and restore")]
        public void StopPreview()
        {
            EditorApplication.update -= UpdatePreview;
            if (previewPose != null && !EditorApplication.isPlaying)
                transform.SetPositionAndRotation(previewFromPosition, previewFromRotation);
            previewPose = null;
        }

        // Plays any pose function outside Play mode (driven by EditorApplication.update); the pose is restored at the end.
        // In Play mode it runs the real coroutine instead.
        public void Preview(Func<float, (Vector3, Quaternion)> poseAt, float duration, bool returns)
        {
            if (EditorApplication.isPlaying)
            {
                Vector3 position = transform.position;
                Quaternion rotation = transform.rotation;
                Play(poseAt, duration, returns ? () => MoveTo(position, rotation, duration) : null);
                return;
            }

            StopPreview();
            previewFromPosition = transform.position;
            previewFromRotation = transform.rotation;
            previewPose = poseAt;
            previewDuration = Mathf.Max(0.01f, duration);
            previewReturns = returns;
            previewStart = EditorApplication.timeSinceStartup;
            EditorApplication.update += UpdatePreview;
        }

        void PreviewMoveToTarget(bool returns)
        {
            if (previewTarget == null)
            {
                XiloLog.Warn(LogCategory, $"{name}: assign Preview Target to preview the motion", this);
                return;
            }
            Preview(MovePose(previewTarget.position, previewTarget.rotation), defaultDuration, returns);
        }

        // Plays forward, holds, then (optionally) plays back.
        void UpdatePreview()
        {
            if (this == null || previewPose == null)
            {
                EditorApplication.update -= UpdatePreview;
                return;
            }

            float elapsed = (float)(EditorApplication.timeSinceStartup - previewStart);
            float total = previewReturns ? previewDuration * 2f + previewHold : previewDuration + previewHold;
            float t = elapsed < previewDuration ? elapsed / previewDuration
                : elapsed < previewDuration + previewHold || !previewReturns ? 1f
                : 1f - (elapsed - previewDuration - previewHold) / previewDuration;

            var (position, rotation) = previewPose(easing.Evaluate(Mathf.Clamp01(t)));
            transform.SetPositionAndRotation(position, rotation);
            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();

            if (elapsed >= total)
                StopPreview();
        }
#endif
    }
}
