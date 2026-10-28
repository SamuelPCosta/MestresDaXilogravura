using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Xilo.Core;

namespace Xilo.UI
{
    [Serializable]
    public sealed class StepText
    {
        public StepId step;
        public string title;
        [TextArea(2, 5)] public string instruction;
    }

    // Shows the title and instruction of the current step. Texts are edited here per step;
    // a missing entry or empty field falls back to the StepDefinition texts.
    public sealed class StepTextView : MonoBehaviour
    {
        [SerializeField] ExperienceFlow flow;
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text instructionLabel;
        [SerializeField] List<StepText> texts = new();

        void OnEnable()
        {
            if (flow == null)
            {
                XiloLog.Error("StepTextView", "ExperienceFlow not assigned", this);
                return;
            }
            flow.StepEntered += HandleStepEntered;
        }

        void OnDisable()
        {
            if (flow != null)
                flow.StepEntered -= HandleStepEntered;
        }

        void HandleStepEntered(StepDefinition definition)
        {
            if (definition == null)
                return;

            StepText entry = texts.Find(text => text.step == definition.Id);
            SetText(titleLabel, entry?.title, definition.Title);
            SetText(instructionLabel, entry?.instruction, definition.Instruction);
        }

        static void SetText(TMP_Text label, string custom, string fallback)
        {
            if (label != null)
                label.text = string.IsNullOrWhiteSpace(custom) ? fallback : custom;
        }
    }
}
