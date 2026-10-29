using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Xilo.EditorTools
{
    // Bakes smoothed normals into UV3 for the outline shader, in the Editor only (no Read/Write, no runtime cost).
    // Models: tag them with the "OutlineNormals" label and every (re)import writes UV3 automatically.
    // Built-in meshes (primitives like the test cube): a smoothed copy is saved as a mesh asset and assigned.
    sealed class OutlineNormalsPostprocessor : AssetPostprocessor
    {
        public const string Label = "OutlineNormals";

        public override uint GetVersion() => 1;

        void OnPostprocessModel(GameObject root)
        {
            if (!AssetDatabase.GetLabels(AssetDatabase.GUIDFromAssetPath(assetPath)).Contains(Label))
                return;

            try
            {
                int count = 0;
                foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
                    count += Bake(filter.sharedMesh);
                foreach (SkinnedMeshRenderer skinned in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    count += Bake(skinned.sharedMesh);
                Debug.Log($"[XiloVR][Outline] Smoothed normals written to UV{SmoothNormals.Channel} on {count} meshes: {assetPath}");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        static int Bake(Mesh mesh)
        {
            if (mesh == null)
                return 0;
            SmoothNormals.WriteToUV(mesh);
            return 1;
        }
    }

    static class OutlineNormalsMenu
    {
        const string MeshFolder = "Assets/Xilo/Meshes/Outline";

        // Tags the selected models and reimports them so the postprocessor writes UV3.
        [MenuItem("Assets/Xilo/Outline: add smoothed normals (UV3)")]
        static void AddToSelectedModels()
        {
            foreach (string path in SelectedModelPaths())
            {
                GUID guid = AssetDatabase.GUIDFromAssetPath(path);
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                string[] labels = AssetDatabase.GetLabels(guid);
                if (!labels.Contains(OutlineNormalsPostprocessor.Label))
                    AssetDatabase.SetLabels(asset, labels.Append(OutlineNormalsPostprocessor.Label).ToArray());
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        [MenuItem("Assets/Xilo/Outline: remove smoothed normals")]
        static void RemoveFromSelectedModels()
        {
            foreach (string path in SelectedModelPaths())
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                string[] labels = AssetDatabase.GetLabels(asset);
                AssetDatabase.SetLabels(asset, labels.Where(label => label != OutlineNormalsPostprocessor.Label).ToArray());
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        [MenuItem("Assets/Xilo/Outline: add smoothed normals (UV3)", true)]
        [MenuItem("Assets/Xilo/Outline: remove smoothed normals", true)]
        static bool HasSelectedModels() => SelectedModelPaths().Any();

        static string[] SelectedModelPaths()
        {
            return Selection.assetGUIDs
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => AssetImporter.GetAtPath(path) is ModelImporter)
                .ToArray();
        }

        // For objects whose mesh is not an imported model (e.g. a primitive cube): saves a smoothed copy and assigns it.
        [MenuItem("GameObject/Xilo/Outline: bake smoothed normals into a mesh copy", false, 0)]
        static void BakeSelectedObjects()
        {
            foreach (GameObject selected in Selection.gameObjects)
            {
                if (!selected.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
                    continue;
                try
                {
                    Mesh copy = UnityEngine.Object.Instantiate(filter.sharedMesh);
                    copy.name = $"{filter.sharedMesh.name}_SmoothNormals";
                    SmoothNormals.WriteToUV(copy);

                    Directory.CreateDirectory(MeshFolder);
                    string path = AssetDatabase.GenerateUniqueAssetPath($"{MeshFolder}/{copy.name}.asset");
                    AssetDatabase.CreateAsset(copy, path);
                    Undo.RecordObject(filter, "Assign smoothed mesh");
                    filter.sharedMesh = copy;
                    Debug.Log($"[XiloVR][Outline] {selected.name} now uses {path}", selected);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, selected);
                }
            }
            AssetDatabase.SaveAssets();
        }
    }
}
