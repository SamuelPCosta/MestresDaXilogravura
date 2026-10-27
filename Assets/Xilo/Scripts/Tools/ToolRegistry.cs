using System.Collections.Generic;
using UnityEngine;
using Xilo.Core;

namespace Xilo.Tools
{
    // Keeps track of the scene tools and enables/highlights the one required by the current step.
    public sealed class ToolRegistry : MonoBehaviour
    {
        const string LogCategory = "Tools";

        [SerializeField] ExperienceFlow flow;
        [SerializeField] Material outlineMaterial;
        [SerializeField] List<XiloTool> tools = new();

        readonly Dictionary<ToolId, XiloTool> toolsById = new();

        public ToolId ActiveToolId { get; private set; } = ToolId.None;

        void Awake()
        {
            foreach (XiloTool tool in tools)
            {
                if (tool == null)
                    continue;
                if (toolsById.ContainsKey(tool.Id))
                {
                    XiloLog.Error(LogCategory, $"Duplicated tool id {tool.Id} on {tool.name}", this);
                    continue;
                }
                toolsById.Add(tool.Id, tool);
                if (tool.TryGetComponent(out ToolHighlighter highlighter))
                    highlighter.SetOutlineMaterial(outlineMaterial);
            }
        }

        void OnEnable()
        {
            if (flow == null)
            {
                XiloLog.Error(LogCategory, "ExperienceFlow not assigned", this);
                return;
            }
            flow.StepEntered += HandleStepEntered;
            flow.Restarted += ReturnAllHome;
        }

        void OnDisable()
        {
            if (flow == null)
                return;
            flow.StepEntered -= HandleStepEntered;
            flow.Restarted -= ReturnAllHome;
        }

        public bool TryGet(ToolId id, out XiloTool tool) => toolsById.TryGetValue(id, out tool);

        // Marks only the given tool as active for the step and moves the highlight to it.
        public void SetActiveTool(ToolId id)
        {
            ActiveToolId = id;
            foreach (XiloTool tool in toolsById.Values)
            {
                bool isActive = tool.Id == id;
                tool.IsActiveForStep = isActive;
                if (!isActive)
                    tool.EndUse();
                if (tool.TryGetComponent(out ToolHighlighter highlighter))
                    highlighter.SetHighlighted(isActive);
            }
            XiloLog.Info(LogCategory, $"Active tool: {id}", this);
        }

        public void ReturnAllHome()
        {
            foreach (XiloTool tool in toolsById.Values)
            {
                if (tool.IsHeld)
                    tool.Grab.interactionManager?.CancelInteractableSelection((UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)tool.Grab);
                if (tool.TryGetComponent(out ToolHomeReturn home))
                    home.SnapHome();
            }
            XiloLog.Info(LogCategory, "All tools returned home", this);
        }

        void HandleStepEntered(StepDefinition definition)
        {
            SetActiveTool(definition != null ? definition.ToolId : ToolId.None);
        }
    }
}
