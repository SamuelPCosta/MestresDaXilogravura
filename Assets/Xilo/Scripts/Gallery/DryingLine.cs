using System;
using System.Collections.Generic;
using UnityEngine;
using Xilo.Core;

namespace Xilo.Gallery
{
    // Hangs the saved prints on the clothes lines: newest on the first sheet of the first line,
    // oldest on the last sheet of the last line. Each sheet gets its art through a property block (shared material untouched).
    public sealed class DryingLine : MonoBehaviour
    {
        const string LogCategory = "DryingLine";

        [SerializeField, Tooltip("Sheets in display order: line 1 sheets 1..n, then line 2, then line 3.")]
        List<Renderer> sheets = new();
        [SerializeField] string artProperty = "_Art";
        [SerializeField, Tooltip("Hide sheets that have no print yet.")] bool hideEmptySheets;

        readonly List<Texture2D> loaded = new();

        void Start()
        {
            Hang();
        }

        void OnDestroy()
        {
            foreach (Texture2D texture in loaded)
                Destroy(texture);
        }

        // Loads the newest prints and assigns one per sheet.
        public void Hang()
        {
            try
            {
                IReadOnlyList<string> prints = GalleryStorage.NewestFirst(sheets.Count);
                for (int i = 0; i < sheets.Count; i++)
                {
                    if (sheets[i] == null)
                        continue;
                    Texture2D art = i < prints.Count ? LoadSafe(prints[i]) : null;
                    Apply(sheets[i], art);
                }
                XiloLog.Info(LogCategory, $"{prints.Count} prints hung on {sheets.Count} sheets ({GalleryStorage.Folder})", this);
            }
            catch (Exception exception)
            {
                XiloLog.Exception(LogCategory, exception, this);
            }
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

        void Apply(Renderer sheet, Texture2D art)
        {
            var block = new MaterialPropertyBlock();
            sheet.GetPropertyBlock(block);
            if (art != null)
                block.SetTexture(artProperty, art);
            sheet.SetPropertyBlock(block);
            if (hideEmptySheets)
                sheet.enabled = art != null;
        }
    }
}
