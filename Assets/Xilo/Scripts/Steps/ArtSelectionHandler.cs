using UnityEngine;
using Xilo.Core;
using Xilo.Painting;
using Xilo.UI;

namespace Xilo.Steps
{
    // Art selection step: shows the art panel and, on confirmation, applies the art and advances.
    public sealed class ArtSelectionHandler : StepHandlerBase
    {
        const string LogCategory = "ArtSelection";

        [SerializeField] ArtSelectionPanel panel;
        [SerializeField] GameObject panelRoot;
        [SerializeField] Workbench workbench;

        ExperienceFlow flow;

        protected override void OnEnter(StepContext context)
        {
            if (panel == null || workbench == null)
            {
                XiloLog.Error(LogCategory, "Missing references", this);
                return;
            }

            flow = context.Flow;
            SetPanelVisible(true);
            panel.ArtChosen += HandleArtChosen;
            ReportProgress(0f);
            ReportCanAdvance(false);
        }

        protected override void OnExit()
        {
            if (panel != null)
                panel.ArtChosen -= HandleArtChosen;
            SetPanelVisible(false);
        }

        public override void ClearProgress()
        {
            ReportProgress(0f);
            ReportCanAdvance(false);
        }

        void HandleArtChosen(ArtEntry entry)
        {
            workbench.ApplyArt(entry.Texture);
            ReportProgress(1f);
            ReportCanAdvance(true);
            flow?.Next();
        }

        void SetPanelVisible(bool visible)
        {
            GameObject root = panelRoot != null ? panelRoot : panel != null ? panel.gameObject : null;
            if (root != null)
                root.SetActive(visible);
        }
    }
}
