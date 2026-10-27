using UnityEngine;

namespace Xilo.Core
{
    // Placeholder handler used until the real handler of a step is implemented.
    public sealed class StubStepHandler : StepHandlerBase
    {
        const string LogCategory = "Stub";

        [SerializeField] bool canAdvanceOnEnter = true;

        protected override void OnEnter(StepContext context)
        {
            XiloLog.Info(LogCategory, $"Enter {Id}", this);
            ReportProgress(canAdvanceOnEnter ? 1f : 0f);
            ReportCanAdvance(canAdvanceOnEnter);
        }

        protected override void OnExit()
        {
            XiloLog.Info(LogCategory, $"Exit {Id}", this);
        }

        public override void ClearProgress()
        {
            XiloLog.Info(LogCategory, $"Clear {Id}", this);
        }
    }
}
