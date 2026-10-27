using System;
using UnityEngine;

namespace Xilo.Core
{
    public abstract class StepHandlerBase : MonoBehaviour, IStepHandler
    {
        [SerializeField] StepDefinition definition;

        public StepDefinition Definition => definition;
        public StepId Id => definition != null ? definition.Id : default;
        public bool IsActive { get; private set; }

        public event Action<float> ProgressChanged;
        public event Action<bool> CanAdvanceChanged;

        public void Enter(StepContext context)
        {
            IsActive = true;
            OnEnter(context);
        }

        public void Exit()
        {
            OnExit();
            IsActive = false;
        }

        public abstract void ClearProgress();

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
