namespace Xilo.Core
{
    public sealed class StepContext
    {
        public ExperienceFlow Flow { get; }

        public StepContext(ExperienceFlow flow)
        {
            Flow = flow;
        }
    }
}
