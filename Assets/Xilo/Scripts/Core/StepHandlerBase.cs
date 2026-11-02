using System;
using System.Collections;
using UnityEngine;

namespace Xilo.Core
{
    public abstract class StepHandlerBase : MonoBehaviour, IStepHandler
    {
        [SerializeField] StepDefinition definition;

        ExperienceFlow flow;
        Coroutine pendingAdvance;

        public StepDefinition Definition => definition;
        public StepId Id => definition != null ? definition.Id : default;
        public bool IsActive { get; private set; }
        protected bool IsAdvancePending => pendingAdvance != null;

        public event Action<float> ProgressChanged;
        public event Action<bool> CanAdvanceChanged;

        public void Enter(StepContext context)
        {
            IsActive = true;
            flow = context.Flow;
            OnEnter(context);
        }

        public void Exit()
        {
            CancelAdvance();
            OnExit();
            IsActive = false;
        }

        // Moves on by itself after a pause (so the result is seen and heard), unless by then the step was left
        // or stillDone no longer holds. Used where the next action follows with the same tool in hand.
        protected void AdvanceAfter(float seconds, Func<bool> stillDone)
        {
            CancelAdvance();
            pendingAdvance = StartCoroutine(AdvanceLater(seconds, stillDone));
        }

        protected void CancelAdvance()
        {
            if (pendingAdvance != null)
                StopCoroutine(pendingAdvance);
            pendingAdvance = null;
        }

        IEnumerator AdvanceLater(float seconds, Func<bool> stillDone)
        {
            yield return new WaitForSeconds(seconds);
            pendingAdvance = null;
            if (!IsActive || !stillDone())
                yield break;
            XiloLog.Info("Step", $"{Id} done, moving on automatically", this);
            flow?.Next();
        }

        public abstract void ClearProgress();

        // Editor debug: completes the step's progress instantly when supported.
        public virtual void DebugFill() { }

        protected abstract void OnEnter(StepContext context);
        protected abstract void OnExit();

        protected void ReportProgress(float progress)
        {
            ProgressChanged?.Invoke(Mathf.Clamp01(progress));
        }

        protected void ReportCanAdvance(bool canAdvance)
        {
            CanAdvanceChanged?.Invoke(canAdvance);
        }
    }
}
