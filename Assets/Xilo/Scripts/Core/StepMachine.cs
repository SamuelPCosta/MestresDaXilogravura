using System;
using System.Collections.Generic;

namespace Xilo.Core
{
    public sealed class StepMachine
    {
        readonly StepId[] order;
        int index;

        public event Action<StepId, StepId> StepChanged;

        public StepId Current => order[index];
        public int Count => order.Length;
        public bool IsFirst => index == 0;
        public bool IsLast => index == order.Length - 1;
        public bool CanGoBack => !IsFirst;
        public bool CanAdvance { get; set; }

        public StepMachine(IReadOnlyList<StepId> stepOrder)
        {
            if (stepOrder == null || stepOrder.Count == 0)
                throw new ArgumentException("Step order must contain at least one step.", nameof(stepOrder));

            order = new StepId[stepOrder.Count];
            for (int i = 0; i < stepOrder.Count; i++)
                order[i] = stepOrder[i];
        }

        // Moves to the next step when the current one is complete (or when forced by debug tools).
        public bool TryAdvance(bool ignoreCondition = false)
        {
            if (IsLast || (!CanAdvance && !ignoreCondition))
                return false;

            MoveTo(index + 1);
            return true;
        }

        public bool TryGoBack()
        {
            if (!CanGoBack)
                return false;

            MoveTo(index - 1);
            return true;
        }

        public void Reset()
        {
            MoveTo(0);
        }

        // Changes the current index, clears the advance flag and notifies listeners.
        void MoveTo(int nextIndex)
        {
            StepId from = Current;
            index = nextIndex;
            CanAdvance = false;
            StepChanged?.Invoke(from, Current);
        }
    }
}
