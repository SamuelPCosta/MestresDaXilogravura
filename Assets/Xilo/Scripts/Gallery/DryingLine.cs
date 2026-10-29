using System;
using System.Collections.Generic;
using UnityEngine;
using Xilo.Core;

namespace Xilo.Gallery
{
    // Hangs the saved prints on the clothes lines: newest on the first sheet of the first line,
    // oldest on the last sheet of the last line. Each sheet gets its art through a property block (shared material untouched).
    // With no saved prints, the welcome arts are hung on the first sheets instead.
    public sealed class DryingLine : MonoBehaviour
    {
        const string LogCategory = "DryingLine";

        [SerializeField, Tooltip("Sheets in display order: line 1 sheets 1..n, then line 2, then line 3.")]
        List<Renderer> sheets = new();
        [SerializeField] string artProperty = "_Art";
        [SerializeField, Tooltip("Hide sheets that have no print yet.")] bool hideEmptySheets;
        [SerializeField, Tooltip("Shown on the first sheets while the saved prints folder is empty.")]
        List<Texture> welcomeArts = new();

        readonly List<Texture2D> loaded = new();

        public bool ShowingWelcome { get; private set; }

        void Start()
        {
            Hang();
        }

        void OnDestroy()
        {
            ReleaseLoaded();
        }

        // Shows the saved prints, or the welcome arts when there are none.
        public void Hang()
        {
            try
            {
                IReadOnlyList<string> prints = GalleryStorage.NewestFirst(sheets.Count);
                if (prints.Count == 0)
                    HangWelcome();
                else
                    HangPrints(prints);
            }
            catch (Exception exception)
            {
                XiloLog.Exception(LogCategory, exception, this);
            }
        }

        // Debug: switches between the welcome arts and the saved prints.
        public void ToggleWelcome()
        {
            try
            {
                if (ShowingWelcome)
                    HangPrints(GalleryStorage.NewestFirst(sheets.Count));
                else
                    HangWelcome();
            }
            catch (Exception exception)
            {
                XiloLog.Exception(LogCategory, exception, this);
            }
        }

        void HangPrints(IReadOnlyList<string> prints)
        {
            ReleaseLoaded();
            for (int i = 0; i < sheets.Count; i++)
            {
                if (sheets[i] != null)
                    Apply(sheets[i], i < prints.Count ? LoadSafe(prints[i]) : null);
            }
            ShowingWelcome = false;
            XiloLog.Info(LogCategory, $"{prints.Count} prints hung on {sheets.Count} sheets ({GalleryStorage.Folder})", this);
        }

        void HangWelcome()
        {
            ReleaseLoaded();
            for (int i = 0; i < sheets.Count; i++)
            {
                if (sheets[i] != null)
                    Apply(sheets[i], i < welcomeArts.Count ? welcomeArts[i] : null);
            }
            ShowingWelcome = true;
            XiloLog.Info(LogCategory, $"Welcome arts hung ({welcomeArts.Count})", this);
        }

        void ReleaseLoaded()
        {
            foreach (Texture2D texture in loaded)
                Destroy(texture);
            loaded.Clear();
        }

        Texture2D LoadSafe(string path)
        {
            try
            {
                Texture2D texture = GalleryStorage.Load(path);
                loaded.Add(texture);
                return texture;
            }
            catch (Exception exception)
            {
                XiloLog.Exception(LogCategory, exception, this);
                return null;
            }
        }

        // An empty block puts the material's own art back on the sheet.
        void Apply(Renderer sheet, Texture art)
        {
            var block = new MaterialPropertyBlock();
            if (art != null)
                block.SetTexture(artProperty, art);
            sheet.SetPropertyBlock(block);
            if (hideEmptySheets)
                sheet.enabled = art != null;
        }
    }
}
