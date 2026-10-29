using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Xilo.Core;
using Xilo.Painting;
using Xilo.Steps;

namespace Xilo.UI
{
    // Maps the notebook brush slider onto the painter's brush scale and shows the current value.
    // The slider (this object's children) only shows in steps whose brush follows it (pencil, gouge).
    public sealed class BrushSizeSlider : MonoBehaviour
    {
        const string LogCategory = "BrushSize";

        [SerializeField] Slider slider;
        [SerializeField] MaskPainter painter;
        [SerializeField] TMP_Text valueLabel;
        [SerializeField, Tooltip("Brush multiplier at the slider minimum.")] float scaleAtMin = 1f;
        [SerializeField, Tooltip("Brush multiplier at the slider maximum.")] float scaleAtMax = 3f;
        [SerializeField, Tooltip("Hides the slider in steps where the brush size doesn't apply.")] ExperienceFlow flow;

        void Awake()
        {
            if (slider == null || painter == null)
            {
                XiloLog.Error(LogCategory, "Slider or painter not assigned", this);
                enabled = false;
                return;
            }

            slider.onValueChanged.AddListener(Apply);
            if (flow != null)
            {
                flow.StepEntered += HandleStepEntered;
                SetShown(false);
            }
        }

        void HandleStepEntered(StepDefinition _)
        {
            SetShown(flow.CurrentHandler is PaintStepHandler { Brush: { UserScalable: true } });
        }

        void SetShown(bool shown)
        {
            foreach (Transform child in transform)
                child.gameObject.SetActive(shown);
        }

        void Start()
        {
            if (enabled)
                Apply(slider.value);
        }

        void OnDestroy()
        {
            if (slider != null)
                slider.onValueChanged.RemoveListener(Apply);
            if (flow != null)
                flow.StepEntered -= HandleStepEntered;
        }

        void Apply(float value)
        {
            painter.SetBrushScale(Mathf.Lerp(scaleAtMin, scaleAtMax, slider.normalizedValue));
            if (valueLabel != null)
                valueLabel.text = slider.wholeNumbers ? Mathf.RoundToInt(value).ToString() : value.ToString("0.#");
        }
    }
}
