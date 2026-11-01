using System.Collections;
using UnityEngine;
using Xilo.Core;

namespace Xilo.Objects
{
    // Golden shine over the sheet as soon as the reveal starts: _TotalAlpha fades in, holds, then fades out.
    // Hidden from the start and when the reveal is undone. Uses a property block, so the material is never changed.
    [RequireComponent(typeof(PaperSheet))]
    public sealed class RevealGlow : MonoBehaviour
    {
        const string LogCategory = "RevealGlow";

        [SerializeField] Renderer glowRenderer;
        [SerializeField] string alphaProperty = "_TotalAlpha";
        [SerializeField, Min(0.01f)] float fadeDuration = 0.25f;
        [Tooltip("Seconds the shine stays fully visible between the fades.")]
        [SerializeField, Min(0f)] float holdDuration = 3f;

        PaperSheet paper;
        MaterialPropertyBlock block;
        int alphaId;
        float alpha;

        void Awake()
        {
            paper = GetComponent<PaperSheet>();
            block = new MaterialPropertyBlock();
            alphaId = Shader.PropertyToID(alphaProperty);
            if (glowRenderer == null)
                XiloLog.Warn(LogCategory, "Glow renderer not assigned", this);
            SetAlpha(0f);
        }

        void OnEnable()
        {
            paper.RevealStarted += Play;
            paper.RevealCleared += Hide;
        }

        void OnDisable()
        {
            paper.RevealStarted -= Play;
            paper.RevealCleared -= Hide;
            Hide();
        }

        public void Play()
        {
            StopAllCoroutines();
            StartCoroutine(Shine());
        }

        public void Hide()
        {
            StopAllCoroutines();
            SetAlpha(0f);
        }

        // Fade in, hold, fade out.
        IEnumerator Shine()
        {
            XiloLog.Info(LogCategory, "Shine", this);
            yield return Fade(1f);
            yield return new WaitForSeconds(holdDuration);
            yield return Fade(0f);
        }

        IEnumerator Fade(float target)
        {
            float from = alpha;
            for (float t = 0f; t < fadeDuration; t += Time.deltaTime)
            {
                SetAlpha(Mathf.Lerp(from, target, t / fadeDuration));
                yield return null;
            }
            SetAlpha(target);
        }

        void SetAlpha(float value)
        {
            alpha = value;
            if (glowRenderer == null)
                return;
            glowRenderer.GetPropertyBlock(block);
            block.SetFloat(alphaId, value);
            glowRenderer.SetPropertyBlock(block);
        }
    }
}
