using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Xilo.Core
{
    public sealed class ExperienceFlow : MonoBehaviour
    {
        const string LogCategory = "Flow";

        [SerializeField] List<StepHandlerBase> handlers = new();

        [Header("Hooks")]
        public UnityEvent<StepId> OnStepEntered = new();
        public UnityEvent<StepId> OnStepExited = new();

        readonly Dictionary<StepId, StepHandlerBase> handlersById = new();
        StepMachine machine;
        StepContext context;

        public event Action<StepDefinition> StepEntered;
        public event Action<float> ProgressChanged;
        public event Action<bool> CanAdvanceChanged;

        public StepId CurrentStep => machine.Current;
        public StepDefinition CurrentDefinition => CurrentHandler.Definition;
        public bool CanAdvance => machine.CanAdvance && !machine.IsLast;
        public bool CanGoBack => machine.CanGoBack;
        public IEnumerable<StepHandlerBase> Handlers => handlers;

        StepHandlerBase CurrentHandler => handlersById[machine.Current];

        // Builds the handler map in StepId order and creates the state machine.
        void Awake()
        {
            try
            {
                BuildHandlerMap();
                machine = new StepMachine(handlersById.Keys.OrderBy(id => id).ToList());
                machine.StepChanged += (from, to) => XiloLog.Info(LogCategory, $"Step: {from} → {to}", this);
                context = new StepContext(this);
            }
            catch (Exception exception)
            {
                XiloLog.Exception(LogCategory, exception, this);
                enabled = false;
            }
        }

        void Start()
        {
            EnterCurrent();
        }

        public void Next() => Advance(false);

        public void ForceNext() => Advance(true);

        // Clears the current step, then returns to the previous one keeping its progress.
        public void Back()
        {
            if (!machine.CanGoBack)
                return;

            try
            {
                CurrentHandler.ClearProgress();
                XiloLog.Info(LogCategory, $"Back: cleared {machine.Current}", this);
                ExitCurrent();
                machine.TryGoBack();
                EnterCurrent();
            }
            catch (Exception exception)
            {
                XiloLog.Exception(LogCategory, exception, this);
            }
        }

        // Clears every step and goes back to the first one.
        public void Restart()
        {
            try
            {
                ExitCurrent();
                foreach (StepHandlerBase handler in handlers)
                    handler.ClearProgress();
                XiloLog.Info(LogCategory, "Restart", this);
                machine.Reset();
                EnterCurrent();
            }
            catch (Exception exception)
            {
                XiloLog.Exception(LogCategory, exception, this);
            }
        }

        // Leaves the current step and enters the next one if allowed.
        void Advance(bool ignoreCondition)
        {
            if (machine.IsLast || (!machine.CanAdvance && !ignoreCondition))
            {
                XiloLog.Info(LogCategory, $"Cannot advance from {machine.Current}", this);
                return;
            }

            try
            {
                ExitCurrent();
                machine.TryAdvance(ignoreCondition);
                EnterCurrent();
            }
            catch (Exception exception)
            {
                XiloLog.Exception(LogCategory, exception, this);
            }
        }

        void EnterCurrent()
        {
            StepHandlerBase handler = CurrentHandler;
            handler.ProgressChanged += HandleProgress;
            handler.CanAdvanceChanged += HandleCanAdvance;
            StepEntered?.Invoke(handler.Definition);
            CanAdvanceChanged?.Invoke(CanAdvance);
            handler.Enter(context);
            OnStepEntered.Invoke(handler.Id);
        }

        void ExitCurrent()
        {
            StepHandlerBase handler = CurrentHandler;
            handler.Exit();
            handler.ProgressChanged -= HandleProgress;
            handler.CanAdvanceChanged -= HandleCanAdvance;
            OnStepExited.Invoke(handler.Id);
        }

        void HandleProgress(float progress)
        {
            ProgressChanged?.Invoke(progress);
        }

        void HandleCanAdvance(bool canAdvance)
        {
            machine.CanAdvance = canAdvance;
            CanAdvanceChanged?.Invoke(CanAdvance);
        }

        // Validates the serialized handlers and indexes them by StepId.
        void BuildHandlerMap()
        {
            handlersById.Clear();
            foreach (StepHandlerBase handler in handlers)
            {
                if (handler == null || handler.Definition == null)
                    throw new InvalidOperationException("ExperienceFlow has a handler without StepDefinition.");
                if (handlersById.ContainsKey(handler.Id))
                    throw new InvalidOperationException($"Duplicated handler for step {handler.Id}.");
                handlersById.Add(handler.Id, handler);
            }

            if (handlersById.Count == 0)
                throw new InvalidOperationException("ExperienceFlow has no handlers.");
        }
    }
}
