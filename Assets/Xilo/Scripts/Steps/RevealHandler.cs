using UnityEngine;
using UnityEngine.Events;
using Xilo.Core;
using Xilo.Objects;

namespace Xilo.Steps
{
    // Reveal step: the sheet turns itself over (PaperSheet.Reveal) and OnPrintRevealed is raised.
    // Saving is offered by the step panel (Phase 6 hooks into it).
    public sealed class RevealHandler : StepHandlerBase
    {
        const string LogCategory = "Reveal";

        [SerializeField] PaperSheet paper;

        [Header("Hooks")]
        public UnityEvent OnPrintRevealed = new();

        Vector3 preRevealPosition;
        Quaternion preRevealRotation;
        bool isRevealed;

        protected override void OnEnter(StepContext context)
        {
            ReportProgress(0f);
            ReportCanAdvance(false);
            if (paper == null)
            {
                XiloLog.Error(LogCategory, "Paper not assigned", this);
                return;
            }

            preRevealPosition = paper.transform.position;
            preRevealRotation = paper.transform.rotation;
            isRevealed = true;
            paper.Reveal(HandleRevealed);
        }

        protected override void OnExit() { }

        // Back/Restart: puts the sheet back in its printing pose.
        public override void ClearProgress()
        {
            if (!isRevealed || paper == null)
                return;
            isRevealed = false;
            paper.Tween.Snap(preRevealPosition, preRevealRotation);
            ReportProgress(0f);
        }

        void HandleRevealed()
        {
            ReportProgress(1f);
            XiloLog.Info(LogCategory, "Print revealed", this);
            OnPrintRevealed.Invoke();
        }
    }
}
