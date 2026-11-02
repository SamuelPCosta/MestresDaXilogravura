using System.Collections;
using UnityEngine;
using Xilo.Core;
using Xilo.Tools;

namespace Xilo.UI
{
    // The closed art folder waits outlined on the desk. Clicking it (ray + trigger) removes the outline and opens it:
    // the spine turns to its open rotation while the cover turns to its own (starting a little later), then the art
    // panel fades in. The panel can't be clicked before it is shown. Restart closes it again.
    [RequireComponent(typeof(RayTriggerClick))]
    public sealed class ArtFolderOpener : MonoBehaviour
    {
        const string LogCategory = "ArtFolder";

        [SerializeField] Transform spine;
        [SerializeField] Transform cover;
        [SerializeField] CanvasGroup panel;
        [SerializeField] ToolHighlighter highlighter;
        [Tooltip("Restart closes the folder again.")]
        [SerializeField] ExperienceFlow flow;

        [Header("Opening (local rotations)")]
        [SerializeField] Vector3 spineOpenEuler = new(0f, 90f, -90f);
        [SerializeField] Vector3 coverOpenEuler = new(0f, 85f, 0f);
        [SerializeField, Min(0.01f)] float openDuration = 1f;
        [Tooltip("The cover starts turning this long after the spine.")]
        [SerializeField, Min(0f)] float coverDelay = 0.25f;
        [Tooltip("Panel fade in, once the cover is open.")]
        [SerializeField, Min(0.01f)] float panelFadeDuration = 0.5f;
        [Tooltip("Ease in and out of the spine and cover turns: 1 = constant speed, higher = slower start and end.")]
        [SerializeField, Range(1f, 5f)] float rotationEase = 3f;

        RayTriggerClick click;
        Quaternion spineClosed;
        Quaternion coverClosed;
        bool opened;

        void Awake()
        {
            click = GetComponent<RayTriggerClick>();
            if (spine == null || cover == null || panel == null)
            {
                XiloLog.Error(LogCategory, "Spine, cover or panel not assigned", this);
                enabled = false;
                return;
            }

            spine.gameObject.SetActive(true);
            cover.gameObject.SetActive(true);
            spineClosed = spine.localRotation;
            coverClosed = cover.localRotation;
            ShowPanel(0f);

            // Listened to for the whole scene: the art step hides the folder when it ends, and a restart must still
            // close it while hidden.
            if (flow != null)
                flow.Restarted += Close;
        }

        void Start()
        {
            if (enabled)
                SetClosed(true);
        }

        void OnDestroy()
        {
            if (flow != null)
                flow.Restarted -= Close;
        }

        void OnEnable() => click.Clicked += Open;

        void OnDisable() => click.Clicked -= Open;

        void Open()
        {
            if (opened)
                return;

            opened = true;
            SetClosed(false);
            XiloLog.Info(LogCategory, "Folder clicked, opening", this);
            StartCoroutine(Opening());
        }

        // Back to the starting state: closed, outlined, clickable, panel hidden.
        public void Close()
        {
            StopAllCoroutines();
            opened = false;
            spine.localRotation = spineClosed;
            cover.localRotation = coverClosed;
            ShowPanel(0f);
            SetClosed(true);
            XiloLog.Info(LogCategory, "Folder closed again", this);
        }

        // Closed = outlined and clickable. Once open, its colliders are off so they never stop the ray before the panel.
        void SetClosed(bool closed)
        {
            click.Clickable = closed;
            if (highlighter != null)
                highlighter.SetHighlighted(closed);
            foreach (Collider part in GetComponentsInChildren<Collider>(true))
                part.enabled = closed;
        }

        // Spine and cover turn together (the cover delayed), then the panel fades in.
        IEnumerator Opening()
        {
            Quaternion spineOpen = Quaternion.Euler(spineOpenEuler);
            Quaternion coverOpen = Quaternion.Euler(coverOpenEuler);
            float total = coverDelay + openDuration;

            for (float elapsed = 0f; elapsed < total; elapsed += Time.deltaTime)
            {
                spine.localRotation = Quaternion.Slerp(spineClosed, spineOpen, RotationEase(elapsed / openDuration));
                cover.localRotation = Quaternion.Slerp(coverClosed, coverOpen, RotationEase((elapsed - coverDelay) / openDuration));
                yield return null;
            }
            spine.localRotation = spineOpen;
            cover.localRotation = coverOpen;

            for (float elapsed = 0f; elapsed < panelFadeDuration; elapsed += Time.deltaTime)
            {
                ShowPanel(Ease(elapsed / panelFadeDuration));
                yield return null;
            }
            ShowPanel(1f);
            XiloLog.Info(LogCategory, "Folder open, art panel shown", this);
        }

        static float Ease(float t) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t));

        // Symmetric ease in/out: t^p / (t^p + (1 - t)^p), slow at both ends, fastest in the middle.
        float RotationEase(float t)
        {
            t = Mathf.Clamp01(t);
            float rising = Mathf.Pow(t, rotationEase);
            return rising / (rising + Mathf.Pow(1f - t, rotationEase));
        }

        void ShowPanel(float alpha)
        {
            panel.alpha = alpha;
            bool shown = alpha >= 1f;
            panel.interactable = shown;
            panel.blocksRaycasts = shown;
        }
    }
}
