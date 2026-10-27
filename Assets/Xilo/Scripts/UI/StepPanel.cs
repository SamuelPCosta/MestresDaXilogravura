using System;
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

        [Header("Hooks")]
        public UnityEvent OnSaveRequested = new();

        StepId currentStep;

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
            Bind(placeSheetButton, flow.Next);
            Bind(showResultButton, flow.Next);
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

            if (backButton != null)
                backButton.interactable = flow.CanGoBack;
            HandleCanAdvance(flow.CanAdvance);
        }

        void HandleCanAdvance(bool canAdvance)
        {
            foreach (Button advance in new[] { nextButton, placeSheetButton, showResultButton })
            {
                if (advance != null)
                    advance.interactable = canAdvance;
            }
        }

        void RequestSave()
        {
            XiloLog.Info(LogCategory, "Save requested", this);
            OnSaveRequested.Invoke();
        }

        static void Bind(Button button, UnityAction action)
        {
            if (button != null)
                button.onClick.AddListener(action);
        }

        static void SetVisible(Button button, bool visible)
        {
            if (button != null)
                button.gameObject.SetActive(visible);
        }
    }
}
