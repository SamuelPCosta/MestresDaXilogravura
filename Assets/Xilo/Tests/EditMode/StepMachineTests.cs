using System;
using NUnit.Framework;
using Xilo.Core;

namespace Xilo.Tests
{
    public sealed class StepMachineTests
    {
        static StepMachine CreateMachine() =>
            new(new[] { StepId.ArtSelection, StepId.Sketching, StepId.Carving });

        [Test]
        public void StartsOnFirstStep()
        {
            StepMachine machine = CreateMachine();

            Assert.AreEqual(StepId.ArtSelection, machine.Current);
            Assert.IsFalse(machine.CanGoBack);
        }

        [Test]
        public void EmptyOrderThrows()
        {
            Assert.Throws<ArgumentException>(() => new StepMachine(Array.Empty<StepId>()));
        }

        [Test]
        public void DoesNotAdvanceWithoutCondition()
        {
            StepMachine machine = CreateMachine();

            Assert.IsFalse(machine.TryAdvance());
            Assert.AreEqual(StepId.ArtSelection, machine.Current);
        }

        [Test]
        public void AdvancesWhenConditionMetAndResetsFlag()
        {
            StepMachine machine = CreateMachine();
            machine.CanAdvance = true;

            Assert.IsTrue(machine.TryAdvance());
            Assert.AreEqual(StepId.Sketching, machine.Current);
            Assert.IsFalse(machine.CanAdvance);
        }

        [Test]
        public void ForcedAdvanceIgnoresCondition()
        {
            StepMachine machine = CreateMachine();

            Assert.IsTrue(machine.TryAdvance(ignoreCondition: true));
            Assert.AreEqual(StepId.Sketching, machine.Current);
        }

        [Test]
        public void DoesNotAdvancePastLastStep()
        {
            StepMachine machine = CreateMachine();
            machine.TryAdvance(true);
            machine.TryAdvance(true);

            Assert.IsTrue(machine.IsLast);
            Assert.IsFalse(machine.TryAdvance(true));
            Assert.AreEqual(StepId.Carving, machine.Current);
        }

        [Test]
        public void GoesBackToPreviousStep()
        {
            StepMachine machine = CreateMachine();
            machine.TryAdvance(true);

            Assert.IsTrue(machine.TryGoBack());
            Assert.AreEqual(StepId.ArtSelection, machine.Current);
            Assert.IsFalse(machine.TryGoBack());
        }

        [Test]
        public void ResetReturnsToFirstStepAndNotifies()
        {
            StepMachine machine = CreateMachine();
            machine.TryAdvance(true);
            machine.TryAdvance(true);
            StepId? notifiedTo = null;
            machine.StepChanged += (_, to) => notifiedTo = to;

            machine.Reset();

            Assert.AreEqual(StepId.ArtSelection, machine.Current);
            Assert.AreEqual(StepId.ArtSelection, notifiedTo);
        }
    }
}
