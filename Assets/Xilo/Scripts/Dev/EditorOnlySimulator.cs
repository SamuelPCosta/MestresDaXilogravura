using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using Xilo.Core;
using CommonUsages = UnityEngine.InputSystem.CommonUsages;

namespace Xilo.Dev
{
    // Keeps the XR Interaction Simulator only in the Editor without a real headset,
    // and refreshes the rig's modality manager once the simulated controllers exist.
    [DefaultExecutionOrder(-1000)]
    public sealed class EditorOnlySimulator : MonoBehaviour
    {
        const string LogCategory = "Simulator";

        [SerializeField] float pollInterval = 0.25f;
        [SerializeField] float warnAfterSeconds = 10f;

        void Awake()
        {
            if (!Application.isEditor || XRSettings.isDeviceActive)
            {
                XiloLog.Info(LogCategory, "Real XR device or build detected, removing simulator", this);
                Destroy(gameObject);
                return;
            }

            XiloLog.Info(LogCategory, "XR Interaction Simulator active", this);
        }

        // Polls until both simulated controllers are tracked, then refreshes the modality managers.
        IEnumerator Start()
        {
            float startTime = Time.realtimeSinceStartup;
            bool warned = false;
            var wait = new WaitForSecondsRealtime(pollInterval);

            while (!AreSimulatedControllersTracked())
            {
                if (!warned && Time.realtimeSinceStartup - startTime > warnAfterSeconds)
                {
                    XiloLog.Warn(LogCategory, "Still waiting for simulated controllers to be tracked", this);
                    warned = true;
                }
                yield return wait;
            }

            RefreshModalityManagers();
        }

        static bool AreSimulatedControllersTracked()
        {
            var controllers = InputSystem.devices.OfType<XRController>().ToList();
            return controllers.Any(c => c.usages.Contains(CommonUsages.LeftHand) && c.isTracked.isPressed)
                && controllers.Any(c => c.usages.Contains(CommonUsages.RightHand) && c.isTracked.isPressed);
        }

        // Re-runs the modality detection so the controller GameObjects get activated.
        void RefreshModalityManagers()
        {
            foreach (XRInputModalityManager manager in FindObjectsByType<XRInputModalityManager>(FindObjectsSortMode.None))
            {
                manager.enabled = false;
                manager.enabled = true;
            }

            XiloLog.Info(LogCategory, $"Controllers ready, modality: {XRInputModalityManager.currentInputMode.Value}", this);
        }
    }
}
