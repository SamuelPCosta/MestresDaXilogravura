using System;
using UnityEngine;
using UnityEngine.UI;
using Xilo.Core;

namespace Xilo.UI
{
    // Browses the art catalog (previous/next) and raises ArtChosen when the user confirms.
    // Kept independent from the flow so it can later be replaced by the "notebook" selector.
    public sealed class ArtSelectionPanel : MonoBehaviour
    {
        const string LogCategory = "ArtPanel";

        [SerializeField] ArtCatalog catalog;
        [SerializeField] Image preview;
        [SerializeField] Button previousButton;
        [SerializeField] Button nextButton;
        [SerializeField] Button chooseButton;

        int index;

        public event Action<ArtEntry> ArtChosen;

        void Awake()
        {
            if (catalog == null || catalog.Count == 0 || preview == null)
            {
                XiloLog.Error(LogCategory, "Catalog or preview not assigned", this);
                enabled = false;
                return;
            }

            previousButton?.onClick.AddListener(ClickGuard.Wrap(() => Browse(-1)));
            nextButton?.onClick.AddListener(ClickGuard.Wrap(() => Browse(1)));
            chooseButton?.onClick.AddListener(ClickGuard.Wrap(Choose));
            Show(0);
        }

        public void Browse(int direction)
        {
            Show((index + direction + catalog.Count) % catalog.Count);
        }

        public void Choose()
        {
            ArtEntry entry = catalog.Arts[index];
            XiloLog.Info(LogCategory, $"Art chosen: {entry.displayName}", this);
            ArtChosen?.Invoke(entry);
        }

        void Show(int newIndex)
        {
            index = newIndex;
            preview.sprite = catalog.Arts[index].sprite;
        }
    }
}
