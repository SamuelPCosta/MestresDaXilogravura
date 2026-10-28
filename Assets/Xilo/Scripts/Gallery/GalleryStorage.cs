using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Xilo.Core;

namespace Xilo.Gallery
{
    // Saved prints on disk: <project>/SavedImages in the Editor, persistentDataPath/SavedImages on the device.
    // Nothing is ever deleted; only the most recent ones (MaxPrints) are listed for display, newest first.
    public static class GalleryStorage
    {
        const string LogCategory = "Gallery";
        public const int MaxPrints = 10;

        public static string Folder => Application.isEditor
            ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "SavedImages"))
            : Path.Combine(Application.persistentDataPath, "SavedImages");

        // Writes the PNG with a sortable timestamp name.
        public static string Save(Texture2D image)
        {
            Directory.CreateDirectory(Folder);
            string path = Path.Combine(Folder, $"print_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            File.WriteAllBytes(path, image.EncodeToPNG());
            XiloLog.Info(LogCategory, $"Print saved: {path}");
            return path;
        }

        public static IReadOnlyList<string> NewestFirst(int count = MaxPrints)
        {
            if (!Directory.Exists(Folder))
                return Array.Empty<string>();
            return new DirectoryInfo(Folder).GetFiles("*.png")
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .Take(count)
                .Select(file => file.FullName)
                .ToList();
        }

        public static Texture2D Load(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { name = Path.GetFileNameWithoutExtension(path) };
            if (!texture.LoadImage(File.ReadAllBytes(path)))
                throw new InvalidDataException($"Not an image: {path}");
            return texture;
        }
    }
}
