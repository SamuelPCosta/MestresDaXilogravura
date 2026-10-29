using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Xilo.Core;

namespace Xilo.UI
{
    // Reveals the elements one after another: each fades in (alpha 0 → 1), then the next waits a pause.
    // Hidden elements don't take clicks, so a button only works once it is visible
    // (it keeps its normal look while fading, never the disabled tint).
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

        void Awake()
        {
            foreach (CanvasGroup element in elements)
                SetVisible(element, 0f);
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

        static void SetVisible(CanvasGroup element, float alpha)
        {
            if (element == null)
                return;
            element.alpha = alpha;
            element.blocksRaycasts = alpha >= 1f;
        }
    }
}
