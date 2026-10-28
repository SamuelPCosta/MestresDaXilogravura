using UnityEngine;
using UnityEngine.Events;
using Xilo.Core;
using Xilo.Objects;
using Xilo.Tools;

namespace Xilo.Steps
{
    // Paper placement step: the "Colocar papel" button animates the sheet onto the matrix, then advances.
    // Tools in the way (the baren) hop up along their local Y while the sheet passes.
    // Coming back to this step returns the sheet to its starting pose.
    public sealed class PaperPlacementHandler : StepHandlerBase, IStepAction
    {
        const string LogCategory = "PaperPlacement";

        [SerializeField] PaperSheet paper;
        [SerializeField] Transform placedPose;
        [SerializeField, Tooltip("Legacy animator that drives the paper transform; disabled while this step moves it.")]
        Animator paperAnimator;

        [Header("Make room")]
        [SerializeField, Tooltip("Objects that hop up (local Y) and come back while the sheet moves.")]
        PoseTween[] makeRoom;
        [SerializeField] float makeRoomHeight = 0.2f;

        [Header("Hooks")]
        public UnityEvent OnPaperPlaced = new();
        public UnityEvent OnPaperRemoved = new();

        ExperienceFlow flow;
        Vector3 homePosition;
        Quaternion homeRotation;
        bool hasHome;
        bool isPlaced;

        protected override void OnEnter(StepContext context)
        {
            if (paper == null || placedPose == null)
            {
                XiloLog.Error(LogCategory, "Paper or placed pose not assigned", this);
                return;
            }

            flow = context.Flow;
            CaptureHome();
            ReportProgress(0f);
            ReportCanAdvance(true);

            if (isPlaced)
                ReturnHome(animated: true);
        }

        protected override void OnExit() { }

        public override void ClearProgress()
        {
            if (isPlaced)
                ReturnHome(animated: false);
            ReportProgress(0f);
        }

        public void PerformAction()
        {
            if (!IsActive || paper == null || paper.Tween.IsMoving)
                return;

            XiloLog.Info(LogCategory, "Placing the sheet on the matrix", this);
            DisableAnimator();
            ReportCanAdvance(false);
            paper.PlayPlaceSound();
            HopObjectsInTheWay(paper.Tween.DefaultDuration);
            paper.Tween.MoveTo(placedPose, HandlePlaced);
        }

        void HandlePlaced()
        {
            isPlaced = true;
            OnPaperPlaced.Invoke();
            ReportProgress(1f);
            ReportCanAdvance(true);
            if (IsActive)
                flow?.Next();
        }

        void ReturnHome(bool animated)
        {
            isPlaced = false;
            if (animated)
            {
                HopObjectsInTheWay(paper.Tween.DefaultDuration);
                paper.Tween.MoveTo(homePosition, homeRotation, paper.Tween.DefaultDuration);
            }
            else
                paper.Tween.Snap(homePosition, homeRotation);
            OnPaperRemoved.Invoke();
            XiloLog.Info(LogCategory, "Sheet returned to the stack", this);
        }

        // Each object goes up along its own local Y and back during the sheet motion; held tools are left alone.
        void HopObjectsInTheWay(float duration)
        {
            if (makeRoom == null)
                return;

            foreach (PoseTween item in makeRoom)
            {
                if (item == null || item.IsMoving || (item.TryGetComponent(out XiloTool tool) && tool.IsHeld))
                    continue;
                item.Bump(item.transform.up * makeRoomHeight, duration);
            }
        }

        // The home pose is read the first time the step starts (after the legacy animator placed it).
        void CaptureHome()
        {
            if (hasHome)
                return;
            homePosition = paper.transform.position;
            homeRotation = paper.transform.rotation;
            hasHome = true;
        }

        void DisableAnimator()
        {
            if (paperAnimator != null && paperAnimator.enabled)
                paperAnimator.enabled = false;
        }

#if UNITY_EDITOR
        [ContextMenu("Preview make room (baren hop)")]
        public void PreviewMakeRoom()
        {
            if (makeRoom == null || paper == null)
                return;
            foreach (PoseTween item in makeRoom)
            {
                if (item != null)
                    item.Preview(item.BumpPose(item.transform.up * makeRoomHeight), paper.Tween.DefaultDuration, returns: false);
            }
        }
#endif
    }
}
