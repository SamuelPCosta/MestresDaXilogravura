using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Xilo.Core;
using Xilo.Objects;

namespace Xilo.UI
{
    // The heavy tutorial notebook. Opening: it appears above the desk (local offset), falls with gravity,
    // hits the desk (other objects hop, impact sound) and bounces once. Closing (its own button): it slides
    // away along local X and is deactivated. Lives on an always-active object, since the notebook starts disabled.
    public sealed class TutorialNotebook : MonoBehaviour
    {
        const string LogCategory = "Tutorial";

        enum State { Hidden, Entering, Shown, Leaving }

        [SerializeField] Transform notebook;
        [SerializeField] Button openButton;
        [SerializeField] Button closeButton;
        [Tooltip("Other buttons that also send the notebook away (place sheet, show result, restart, redo step).")]
        [SerializeField] List<Button> alsoCloseOn = new();
        [Tooltip("The open button only shows up after the art is chosen (hidden again on restart).")]
        [SerializeField] ExperienceFlow flow;

        [Header("Open button tuck")]
        [Tooltip("Pos Y the open button slides to while the notebook is out (it comes back when the notebook leaves).")]
        [SerializeField] float openButtonHiddenY = -130f;
        [SerializeField] float openButtonSlideDuration = 0.3f;

        [Header("Fall")]
        [Tooltip("Where it appears, relative to its resting place (local). (0, 0, -1) = 1 m above the desk.")]
        [SerializeField] Vector3 dropOffset = new(0f, 0f, -1f);
        [Tooltip("Turns its face towards the player while falling (degrees); straightened on the impact.")]
        [SerializeField, Range(0f, 60f)] float facePlayerAngle = 20f;
        [Tooltip("Who it faces while falling. Empty = main camera.")]
        [SerializeField] Transform viewer;
        [Tooltip("Extra local tilt (degrees) while falling; also straightened on the impact.")]
        [SerializeField] Vector3 fallTilt = Vector3.zero;
        [SerializeField] float fallDuration = 0.4f;
        [Tooltip("How the fall speeds up: 2 = gravity, higher = slams harder at the end (feels heavier).")]
        [SerializeField, Range(1f, 5f)] float fallWeight = 3f;
        [Tooltip("Bounce height after the impact (meters).")]
        [SerializeField] float bounceHeight = 0.012f;
        [SerializeField] float bounceDuration = 0.14f;

        [Header("Impact")]
        [SerializeField] ImpactHop impactHop;
        [SerializeField] AudioSource impactAudio;
        [Tooltip("Optional: played on the impact.")]
        [SerializeField] AudioClip impactClip;

        [Header("Exit")]
        [Tooltip("Where it goes when closed, relative to its resting place (local). Negative Z = up from the desk.")]
        [SerializeField] Vector3 exitOffset = new(3f, 0f, -0.4f);
        [Tooltip("Rotation added on the way out (local degrees, from the resting rotation).")]
        [SerializeField] Vector3 exitTurn = new(0f, -50f, 0f);
        [SerializeField] float exitDuration = 0.4f;

        State state = State.Hidden;
        bool closeWhenLanded;
        float openButtonShownY;
        Coroutine openButtonSlide;
        Vector3 restPosition;
        Quaternion restRotation;
        Vector3 restEuler;

        void Awake()
        {
            if (notebook == null)
            {
                XiloLog.Error(LogCategory, "Notebook not assigned", this);
                enabled = false;
                return;
            }

            restPosition = notebook.localPosition;
            restRotation = notebook.localRotation;
            restEuler = notebook.localEulerAngles;
            notebook.gameObject.SetActive(false);

            if (openButton != null)
            {
                openButtonShownY = ((RectTransform)openButton.transform).anchoredPosition.y;
                openButton.onClick.AddListener(ClickGuard.Wrap(Open));
            }
            if (closeButton != null)
                closeButton.onClick.AddListener(ClickGuard.Wrap(Close));

            // Not wrapped by the ClickGuard: these buttons already run their own guarded action this frame,
            // and closing twice is harmless.
            foreach (Button button in alsoCloseOn)
            {
                if (button != null)
                    button.onClick.AddListener(Close);
            }

            if (flow != null && openButton != null)
            {
                openButton.gameObject.SetActive(false);
                flow.StepEntered += HandleStepEntered;
            }
        }

        void OnDestroy()
        {
            if (flow != null)
                flow.StepEntered -= HandleStepEntered;
        }

        void HandleStepEntered(StepDefinition step)
        {
            openButton.gameObject.SetActive(step.Id != StepId.ArtSelection);
        }

        public void Open()
        {
            if (state != State.Hidden)
                return;
            state = State.Entering;
            closeWhenLanded = false;
            SlideOpenButton(openButtonHiddenY);
            XiloLog.Info(LogCategory, "Opening tutorial", this);
            notebook.gameObject.SetActive(true);
            StartCoroutine(Run(Enter, State.Shown));
        }

        // Sends the notebook away; if it is still falling, it leaves as soon as it lands.
        public void Close()
        {
            if (state == State.Entering)
            {
                closeWhenLanded = true;
                return;
            }
            if (state != State.Shown)
                return;
            state = State.Leaving;
            SlideOpenButton(openButtonShownY);
            XiloLog.Info(LogCategory, "Closing tutorial", this);
            StartCoroutine(Run(Leave, State.Hidden));
        }

        // Runs a motion (nested animations included) and always ends in a consistent state, even if it fails.
        IEnumerator Run(Func<IEnumerator> motion, State endState)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(motion());
            while (stack.Count > 0)
            {
                bool advanced;
                try
                {
                    advanced = stack.Peek().MoveNext();
                }
                catch (Exception exception)
                {
                    XiloLog.Exception(LogCategory, exception, this);
                    break;
                }

                if (!advanced)
                    stack.Pop();
                else if (stack.Peek().Current is IEnumerator nested)
                    stack.Push(nested);
                else
                    yield return stack.Peek().Current;
            }

            if (endState == State.Hidden)
                Hide();
            else
                Place(restPosition, restRotation);
            state = endState;

            if (state == State.Shown && closeWhenLanded)
            {
                closeWhenLanded = false;
                Close();
            }
        }

        // Falls speeding up (heavier = slams at the end), turned towards the player; straightens on the hit, then bounces once.
        IEnumerator Enter()
        {
            Vector3 start = restPosition + dropOffset;
            Quaternion tilted = FacingPlayer(start) * restRotation * Quaternion.Euler(fallTilt);
            yield return Animate(fallDuration, t =>
            {
                float fall = Mathf.Pow(t, fallWeight);
                Place(Vector3.LerpUnclamped(start, restPosition, fall), Quaternion.Slerp(tilted, restRotation, fall));
            });

            Impact();

            Vector3 up = DeskUp * bounceHeight;
            yield return Animate(bounceDuration, t => Place(restPosition + up * (4f * t * (1f - t)), restRotation));
        }

        // Rotation (parent space) that turns the notebook's face from the desk normal towards the viewer, capped.
        Quaternion FacingPlayer(Vector3 localStart)
        {
            Transform target = viewer != null ? viewer : (Camera.main != null ? Camera.main.transform : null);
            if (target == null || notebook.parent == null)
                return Quaternion.identity;
            Vector3 startWorld = notebook.parent.TransformPoint(localStart);
            Vector3 toViewer = notebook.parent.InverseTransformDirection(target.position - startWorld);
            return Quaternion.RotateTowards(Quaternion.identity, Quaternion.FromToRotation(DeskUp, toViewer), facePlayerAngle);
        }

        // Away from the desk surface, in the notebook's parent space (where it falls from).
        Vector3 DeskUp => dropOffset.sqrMagnitude > 0f ? dropOffset.normalized : Vector3.back;

        // Slides away along the exit offset (rising) while turning, speeding up.
        IEnumerator Leave()
        {
            Vector3 start = notebook.localPosition;
            Quaternion startRotation = notebook.localRotation;
            Vector3 end = restPosition + exitOffset;
            Quaternion endRotation = Quaternion.Euler(restEuler + exitTurn);
            yield return Animate(exitDuration, t =>
            {
                float move = t * t;
                Place(Vector3.LerpUnclamped(start, end, move), Quaternion.Slerp(startRotation, endRotation, move));
            });
        }

        void Impact()
        {
            if (impactHop != null && notebook.parent != null)
                impactHop.Hit(notebook.parent.TransformDirection(DeskUp), notebook.position);
            if (impactAudio != null && impactClip != null)
                impactAudio.PlayOneShot(impactClip);
        }

        // Eases the open button's Pos Y; a hidden (inactive) button just jumps there.
        void SlideOpenButton(float targetY)
        {
            if (openButton == null)
                return;
            var rect = (RectTransform)openButton.transform;
            if (openButtonSlide != null)
                StopCoroutine(openButtonSlide);

            if (!openButton.gameObject.activeInHierarchy)
            {
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, targetY);
                return;
            }

            float startY = rect.anchoredPosition.y;
            openButtonSlide = StartCoroutine(Animate(openButtonSlideDuration, t =>
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, Mathf.Lerp(startY, targetY, Mathf.SmoothStep(0f, 1f, t)))));
        }

        IEnumerator Animate(float duration, Action<float> apply)
        {
            float length = Mathf.Max(0.01f, duration);
            for (float elapsed = 0f; elapsed < length; elapsed += Time.deltaTime)
            {
                apply(elapsed / length);
                yield return null;
            }
            apply(1f);
        }

        void Hide()
        {
            notebook.gameObject.SetActive(false);
            Place(restPosition, restRotation);
        }

        void Place(Vector3 localPosition, Quaternion localRotation)
        {
            notebook.SetLocalPositionAndRotation(localPosition, localRotation);
        }
    }
}
