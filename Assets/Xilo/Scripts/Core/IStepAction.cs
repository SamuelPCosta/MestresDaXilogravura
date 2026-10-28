namespace Xilo.Core
{
    // A step that runs its own action (e.g. an animation) before advancing, triggered by a contextual button.
    public interface IStepAction
    {
        void PerformAction();
    }
}
