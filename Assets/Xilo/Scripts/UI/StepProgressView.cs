using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Xilo.Core;

namespace Xilo.UI
{
    // Shows the current step title and its coverage progress on a world-space slider.
    // The label switches color at the fill edge: two masked copies of it are drawn inside the bar, one over the
    // filled part (fill color) and one over the empty part (label color), splitting letters exactly at the edge.
    // The original label stays as the style template (font, size, alignment) and is hidden at runtime.
    public sealed class StepProgressView : MonoBehaviour
    {
        [SerializeField] ExperienceFlow flow;
        [SerializeField] Slider slider;
        [SerializeField] TextMeshProUGUI label;
        [Tooltip("Text color over the filled part of the bar (the empty part keeps the label color).")]
        [SerializeField] Color textOnFillColor = new(0.358f, 0.288f, 0.255f, 1f);

        string currentTitle = string.Empty;
        TextMeshProUGUI onFill;
        TextMeshProUGUI onEmpty;
        RectTransform emptyArea;

        void Awake()
        {
            CreateSplitLabel();
        }

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

        // Keeps both copies exactly where the label is while their masks resize with the progress.
        void LateUpdate()
        {
            if (onFill == null)
                return;
            float edge = slider.fillRect.anchorMax.x;
            emptyArea.anchorMin = new Vector2(edge, 0f);
            Align(onFill.rectTransform);
            Align(onEmpty.rectTransform);
        }

        // The bar (with its labels inside) is hidden while the art is being chosen.
        void HandleStepEntered(StepDefinition definition)
        {
            currentTitle = definition != null ? definition.Title : string.Empty;
            if (slider != null)
                slider.gameObject.SetActive(definition == null || definition.Id != StepId.ArtSelection);
            HandleProgress(0f);
        }

        void HandleProgress(float progress)
        {
            if (slider != null)
                slider.value = progress;
            string text = $"{currentTitle} — {progress:P0}";
            if (label != null)
                label.text = text;
            if (onFill != null)
            {
                onFill.text = text;
                onEmpty.text = text;
            }
        }

        // Builds the masked copies: one inside the fill, one inside a twin area covering the rest of the bar.
        void CreateSplitLabel()
        {
            if (label == null || slider == null || slider.fillRect == null)
                return;

            RectTransform fill = slider.fillRect;
            if (!fill.TryGetComponent(out RectMask2D _))
                fill.gameObject.AddComponent<RectMask2D>();

            var empty = new GameObject("Empty Area (label mask)", typeof(RectTransform), typeof(RectMask2D));
            emptyArea = (RectTransform)empty.transform;
            emptyArea.SetParent(fill.parent, false);
            emptyArea.anchorMin = new Vector2(fill.anchorMax.x, 0f);
            emptyArea.anchorMax = Vector2.one;
            emptyArea.offsetMin = emptyArea.offsetMax = Vector2.zero;

            onFill = CloneLabel(fill, textOnFillColor);
            onEmpty = CloneLabel(emptyArea, label.color);
            label.enabled = false;
        }

        TextMeshProUGUI CloneLabel(RectTransform parent, Color color)
        {
            TextMeshProUGUI copy = Instantiate(label, parent);
            copy.name = $"{label.name} ({parent.name})";
            copy.color = color;
            copy.raycastTarget = false;
            RectTransform rect = copy.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one * (label.rectTransform.lossyScale.x / Mathf.Max(1e-6f, parent.lossyScale.x));
            return copy;
        }

        void Align(RectTransform copy)
        {
            RectTransform source = label.rectTransform;
            copy.SetPositionAndRotation(source.TransformPoint(source.rect.center), source.rotation);
            copy.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, source.rect.width);
            copy.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, source.rect.height);
        }
    }
}
