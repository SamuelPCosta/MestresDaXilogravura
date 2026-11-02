using UnityEngine;
using Xilo.Core;
using Xilo.Tools;
using Xilo.UI;

namespace Xilo.Steps
{
    // During the paper placement step the paper stack is outlined and pointing at it with the ray and pulling the
    // trigger brings a sheet onto the matrix. After the click (or in any other step) the outline goes away and
    // the stack stops reacting. Every collider under this object belongs to the stack.
    [RequireComponent(typeof(RayTriggerClick))]
    public sealed class PaperStackPicker : MonoBehaviour
    {
        const string LogCategory = "PaperStack";

        [SerializeField] ExperienceFlow flow;
        [SerializeField] PaperPlacementHandler placement;
        [SerializeField] ToolHighlighter highlighter;

        RayTriggerClick click;

        void Awake()
        {
            click = GetComponent<RayTriggerClick>();
            if (flow == null || placement == null)
            {
                XiloLog.Error(LogCategory, "ExperienceFlow or PaperPlacementHandler not assigned", this);
                enabled = false;
            }
        }

        void OnEnable()
        {
            click.Clicked += Pick;
            flow.StepEntered += HandleStepEntered;
            Arm(false);
        }

        void OnDisable()
        {
            click.Clicked -= Pick;
            if (flow != null)
                flow.StepEntered -= HandleStepEntered;
            Arm(false);
        }

        void HandleStepEntered(StepDefinition definition) => Arm(definition != null && definition.Id == StepId.PaperPlacement);

        void Pick()
        {
            if (!placement.CanPlace)
            {
                XiloLog.Info(LogCategory, "Stack clicked while the sheet is still moving, ignored", this);
                return;
            }

            XiloLog.Info(LogCategory, "Stack clicked, bringing a sheet", this);
            Arm(false);
            placement.PerformAction();
        }

        // Armed = outlined and clickable; otherwise the stack never catches the ray.
        void Arm(bool on)
        {
            click.Clickable = on;
            if (highlighter != null)
                highlighter.SetHighlighted(on);
        }
    }
}
