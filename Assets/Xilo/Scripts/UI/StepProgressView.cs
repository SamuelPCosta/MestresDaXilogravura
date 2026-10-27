using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Xilo.Core;

namespace Xilo.UI
{
    // Shows the current step title and its coverage progress on a world-space slider.
    public sealed class StepProgressView : MonoBehaviour
    {
        [SerializeField] ExperienceFlow flow;
        [SerializeField] Slider slider;
        [SerializeField] TextMeshProUGUI label;

        string currentTitle = string.Empty;

        void OnEnable()
        {
            if (flow == null)
            {
                XiloLog.Error("ProgressView", "ExperienceFlow not assigned", this);
                return;
            }
            flow.StepEntered += HandleStepEntered;
            flow.ProgressChanged += HandleProgress;
        }

        void OnDisable()
        {
            if (flow == null)
                return;
            flow.StepEntered -= HandleStepEntered;
            flow.ProgressChanged -= HandleProgress;
        }

        void HandleStepEntered(StepDefinition definition)
        {
            currentTitle = definition != null ? definition.Title : string.Empty;
            HandleProgress(0f);
        }

        void HandleProgress(float progress)
        {
            if (slider != null)
                slider.value = progress;
            if (label != null)
                label.text = $"{currentTitle} — {progress:P0}";
        }
    }
}
