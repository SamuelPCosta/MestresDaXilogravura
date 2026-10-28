using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
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
        [SerializeField] Button placeSheetButton;
        [SerializeField] Button showResultButton;
        [SerializeField] Button saveButton;
        [SerializeField] Button restartButton;

        [Header("Feedback")]
        [SerializeField, Tooltip("Plays when Next (or Show result) goes from disabled to enabled.")] AudioSource feedbackAudio;
        [SerializeField] AudioClip unlockedClip;

        [Header("Hooks")]
        public UnityEvent OnSaveRequested = new();

        StepId currentStep;
        bool advanceEnabled;

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
            Bind(placeSheetButton, flow.PerformStepAction);
            Bind(showResultButton, flow.PerformStepAction);
            Bind(saveButton, RequestSave);
            Bind(restartButton, flow.Restart);
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

            SetVisible(nextButton, currentStep is not (StepId.ArtSelection or StepId.PaperPlacement or StepId.Pressing or StepId.Reveal));
            SetVisible(placeSheetButton, currentStep == StepId.PaperPlacement);
            SetVisible(showResultButton, currentStep == StepId.Pressing);
            SetVisible(saveButton, currentStep == StepId.Reveal);
            SetVisible(restartButton, true);
            SetVisible(backButton, true);
            RefreshGroups();

            if (backButton != null)
                backButton.interactable = flow.CanGoBack;
            advanceEnabled = flow.CanAdvance;
            HandleCanAdvance(flow.CanAdvance);
        }

        // Updates the advance buttons and plays the unlock sound on a disabled → enabled change of a visible Next/Show result.
        void HandleCanAdvance(bool canAdvance)
        {
            foreach (Button advance in new[] { nextButton, placeSheetButton, showResultButton })
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
            return new[] { backButton, nextButton, placeSheetButton, showResultButton, saveButton, restartButton };
        }

        static void SetVisible(Button button, bool visible)
        {
            if (button != null)
                button.gameObject.SetActive(visible);
        }
    }
}
