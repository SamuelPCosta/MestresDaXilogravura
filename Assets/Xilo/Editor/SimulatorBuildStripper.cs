using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using Xilo.Dev;

namespace Xilo.EditorTools
{
    // Removes the XR Interaction Simulator from every scene of a player build. Left in, its components wake up on the
    // headset and remove the real HMD device, so the head and controllers stop tracking.
    sealed class SimulatorBuildStripper : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        // Called for each scene while building the player (report is null when entering Play mode: keep it there).
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (report == null)
                return;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (EditorOnlySimulator simulator in root.GetComponentsInChildren<EditorOnlySimulator>(true))
                {
                    Debug.Log($"[Build] Stripping XR Interaction Simulator '{simulator.name}' from {scene.name}");
                    Object.DestroyImmediate(simulator.gameObject);
                }
            }
        }
    }
}
