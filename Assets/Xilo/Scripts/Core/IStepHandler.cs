using System;

namespace Xilo.Core
{
    public interface IStepHandler
    {
        StepId Id { get; }
        StepDefinition Definition { get; }

        event Action<float> ProgressChanged;
        event Action<bool> CanAdvanceChanged;

        void Enter(StepContext context);
        void Exit();
        void ClearProgress();
    }
}
