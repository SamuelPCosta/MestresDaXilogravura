using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Xilo.Core;

namespace Xilo.UI
{
    // Reveals the elements one after another: each fades in (alpha 0 → 1), then the next waits a pause.
    // Hidden elements don't take clicks, so a button only works once it is visible
    // (it keeps its normal look while fading, never the disabled tint).
    // The skip button is the exception: visible from the start with the skip text, it switches to the
    // final text when its turn in the sequence comes.
    public sealed class FadeSequence : MonoBehaviour
    {
        const string LogCategory = "FadeSequence";

        [Tooltip("Shown in this order.")]
        [SerializeField] List<CanvasGroup> elements = new();
        [Tooltip("Fade in time of each element (seconds).")]
        [SerializeField] float fadeDuration = 1.5f;
        [Tooltip("Pause after an element is visible before the next one starts (seconds).")]
        [SerializeField] float pauseBetween = 5f;
        [Tooltip("Wait before the first element (seconds).")]
        [SerializeField] float startDelay = 0.5f;

        [Header("Skip button")]
        [Tooltip("Element of the list that is always visible; its label changes when its turn comes.")]
        [SerializeField] CanvasGroup skipButton;
        [Tooltip("Empty = the first text inside the skip button.")]
        [SerializeField] TMP_Text skipLabel;
        [SerializeField] string skipText = "Pular";
        [SerializeField] string finalText = "Vamos lá";

        void Awake()
        {
            foreach (CanvasGroup element in elements)
                SetVisible(element, element != null && element == skipButton ? 1f : 0f);
            if (skipLabel == null && skipButton != null)
                skipLabel = skipButton.GetComponentInChildren<TMP_Text>(true);
            if (skipButton != null && skipLabel == null)
                XiloLog.Warn(LogCategory, $"{skipButton.name} has no text to switch", this);
            SetSkipLabel(skipText);
        }

        IEnumerator Start()
        {
            yield return new WaitForSeconds(startDelay);
            for (int i = 0; i < elements.Count; i++)
            {
                if (elements[i] == null)
                    continue;
                if (i > 0)
                    yield return new WaitForSeconds(pauseBetween);
                if (elements[i] == skipButton)
                {
                    yield return new WaitForSeconds(fadeDuration);
                    SetSkipLabel(finalText);
                    XiloLog.Info(LogCategory, $"{skipButton.name} now reads \"{finalText}\"", this);
                    continue;
                }
                yield return FadeIn(elements[i]);
                XiloLog.Info(LogCategory, $"{elements[i].name} visible", this);
            }
        }

        IEnumerator FadeIn(CanvasGroup element)
        {
            float length = Mathf.Max(0.01f, fadeDuration);
            for (float elapsed = 0f; elapsed < length; elapsed += Time.deltaTime)
            {
                SetVisible(element, Mathf.SmoothStep(0f, 1f, elapsed / length));
                yield return null;
            }
            SetVisible(element, 1f);
        }

        void SetSkipLabel(string text)
        {
            if (skipLabel != null)
                skipLabel.text = text;
        }

        static void SetVisible(CanvasGroup element, float alpha)
        {
            if (element == null)
                return;
            element.alpha = alpha;
            element.blocksRaycasts = alpha >= 1f;
        }
    }
}
