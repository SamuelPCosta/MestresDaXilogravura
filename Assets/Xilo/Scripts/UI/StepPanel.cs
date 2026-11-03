using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Xilo.Core;

namespace Xilo.UI
{
    // Flow buttons of the workshop. Only reacts to ExperienceFlow events; layout and style are free.
    public sealed class StepPanel : MonoBehaviour
    {
        const string LogCategory = "StepPanel";

        [SerializeField] ExperienceFlow flow;

        [Header("Buttons")]
        [SerializeField] Button backButton;
        [SerializeField] Button nextButton;
        [SerializeField] Button showResultButton;
        [SerializeField] Button restartButton;
        [Tooltip("Shown only after the reveal; holds Save and Exit, which stay active inside it.")]
        [SerializeField] GameObject finalButtons;
        [SerializeField] Button saveButton;
        [Tooltip("Back to the menu scene.")]
        [SerializeField] Button exitButton;
        [SerializeField] string menuScene = "Menu";

        [Header("Feedback")]
        [SerializeField, Tooltip("Plays when Next (or Show result) goes from disabled to enabled.")] AudioSource feedbackAudio;
        [SerializeField] AudioClip unlockedClip;

        [Header("Hooks")]
        public UnityEvent OnSaveRequested = new();

        StepId currentStep;
        bool advanceEnabled;
        bool isLeaving;

        void Awake()
        {
            if (flow == null)
            {
                XiloLog.Error(LogCategory, "ExperienceFlow not assigned", this);
                enabled = false;
                return;
            }

            Bind(backButton, flow.Back);
            Bind(nextButton, flow.Next);
            Bind(showResultButton, flow.PerformStepAction);
            Bind(saveButton, RequestSave);
            Bind(restartButton, flow.Restart);
            Bind(exitButton, ExitToMenu);
            if (finalButtons == null)
                XiloLog.Warn(LogCategory, "Final buttons (Save/Exit) not assigned", this);
            else
                finalButtons.SetActive(false);
        }

        void OnEnable()
        {
            if (flow == null)
                return;
            flow.StepEntered += HandleStepEntered;
            flow.CanAdvanceChanged += HandleCanAdvance;
        }

        void OnDisable()
        {
            if (flow == null)
                return;
            flow.StepEntered -= HandleStepEntered;
            flow.CanAdvanceChanged -= HandleCanAdvance;
        }

        // Shows the buttons that belong to the step: contextual "advance" buttons replace Next where needed.
        void HandleStepEntered(StepDefinition definition)
        {
            currentStep = definition != null ? definition.Id : StepId.ArtSelection;

            SetVisible(nextButton, currentStep is not (StepId.ArtSelection or StepId.Pressing or StepId.Reveal));
            SetVisible(showResultButton, currentStep == StepId.Pressing);
            if (finalButtons != null)
                finalButtons.SetActive(currentStep == StepId.Reveal);
            SetVisible(restartButton, true);
            SetVisible(backButton, true);
            RefreshGroups();
            RebuildButtonLayouts();

            if (backButton != null)
                backButton.interactable = flow.CanGoBack;
            advanceEnabled = flow.CanAdvance;
            HandleCanAdvance(flow.CanAdvance);
        }

        // Updates the advance buttons and plays the unlock sound on a disabled → enabled change of a visible Next/Show result.
        void HandleCanAdvance(bool canAdvance)
        {
            foreach (Button advance in new[] { nextButton, showResultButton })
            {
                if (advance != null)
                    advance.interactable = canAdvance;
            }

            if (canAdvance && !advanceEnabled && (IsShown(nextButton) || IsShown(showResultButton)))
                PlayUnlocked();
            advanceEnabled = canAdvance;
        }

        void PlayUnlocked()
        {
            if (feedbackAudio == null || unlockedClip == null)
            {
                XiloLog.Warn(LogCategory, "Unlock sound not assigned", this);
                return;
            }
            feedbackAudio.PlayOneShot(unlockedClip);
        }

        static bool IsShown(Button button) => button != null && button.gameObject.activeInHierarchy;

        void RequestSave()
        {
            XiloLog.Info(LogCategory, "Save requested", this);
            OnSaveRequested.Invoke();
        }

        // Loads the menu once, ignoring repeated clicks while it loads.
        void ExitToMenu()
        {
            if (isLeaving)
                return;

            try
            {
                isLeaving = true;
                XiloLog.Info(LogCategory, $"Leaving to {menuScene}", this);
                SceneManager.LoadSceneAsync(menuScene);
            }
            catch (Exception exception)
            {
                isLeaving = false;
                XiloLog.Exception(LogCategory, exception, this);
            }
        }

        static void Bind(Button button, UnityAction action)
        {
            if (button != null)
                button.onClick.AddListener(ClickGuard.Wrap(action));
        }

        // Button containers (e.g. Basic, Conditionals) are shown only while they hold a visible button.
        void RefreshGroups()
        {
            var groups = new HashSet<Transform>();
            foreach (Button button in AllButtons())
            {
                if (button != null && button.transform.parent != transform)
                    groups.Add(button.transform.parent);
            }

            foreach (Transform group in groups)
                group.gameObject.SetActive(HasVisibleButton(group));
        }

        static bool HasVisibleButton(Transform group)
        {
            foreach (Transform child in group)
            {
                if (child.gameObject.activeSelf && child.GetComponent<Button>() != null)
                    return true;
            }
            return false;
        }

        IEnumerable<Button> AllButtons()
        {
            return new[] { backButton, nextButton, showResultButton, restartButton };
        }

        // Buttons shown for the first time (e.g. Show result) get laid out before their label has measured itself,
        // so the icon ends up under the text. Measures the labels and rebuilds the icon+text layout now and next frame.
        void RebuildButtonLayouts()
        {
            RebuildVisibleButtons();
            if (isActiveAndEnabled)
                StartCoroutine(RebuildNextFrame());
        }

        IEnumerator RebuildNextFrame()
        {
            yield return null;
            RebuildVisibleButtons();
        }

        void RebuildVisibleButtons()
        {
            foreach (Button button in AllButtons().Append(saveButton).Append(exitButton))
            {
                if (!IsShown(button))
                    continue;
                foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>())
                    label.ForceMeshUpdate();
                foreach (LayoutGroup layout in button.GetComponentsInChildren<LayoutGroup>())
                    LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)layout.transform);
            }
        }

        static void SetVisible(Button button, bool visible)
        {
            if (button != null)
                button.gameObject.SetActive(visible);
        }
    }
}
