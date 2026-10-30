using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using Xilo.Core;
using Xilo.Rig;
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
        [Tooltip("How long the start pitch is re-applied if the simulator resets the head while starting up.")]
        [SerializeField] float pitchWatchSeconds = 3f;

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
            StartCoroutine(ApplyStartPitchWhenReady());

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

        // Waits for the simulator to initialise its head state (first frames), applies the pitch, then watches for a
        // while: the simulator may reset its devices once more while starting up (e.g. on a modality change).
        IEnumerator ApplyStartPitchWhenReady()
        {
            yield return null;
            yield return null;
            ApplyStartPitch();

            float watchUntil = Time.realtimeSinceStartup + pitchWatchSeconds;
            while (Time.realtimeSinceStartup < watchUntil)
            {
                yield return null;
                if (HeadWasReset())
                {
                    XiloLog.Info(LogCategory, "Simulator reset the head, applying the start pitch again", this);
                    ApplyStartPitch();
                }
            }
        }

        // True when the simulated head is back to level (its startup reset) while a pitch was authored.
        bool HeadWasReset()
        {
            var rig = FindFirstObjectByType<RigStartPose>();
            var simulator = FindFirstObjectByType<XRInteractionSimulator>();
            FieldInfo eulerField = typeof(XRInteractionSimulator).GetField("m_CenterEyeEuler", BindingFlags.Instance | BindingFlags.NonPublic);
            if (rig == null || simulator == null || eulerField == null || Mathf.Approximately(rig.AuthoredPitch, 0f))
                return false;
            var euler = (Vector3)eulerField.GetValue(simulator);
            return Mathf.Approximately(euler.x, 0f) && Mathf.Approximately(euler.y, 0f);
        }

        // Starts the simulated head looking down by the pitch authored on the Camera Offset (the offset itself stays
        // level so walking keeps to the floor plane). The simulator has no public API for its head pose, so its
        // private fields are set by reflection; Editor-only, and it just logs a warning if the package changes.
        void ApplyStartPitch()
        {
            var rig = FindFirstObjectByType<RigStartPose>();
            var simulator = FindFirstObjectByType<XRInteractionSimulator>();
            if (rig == null || simulator == null || Mathf.Approximately(rig.AuthoredPitch, 0f))
                return;

            try
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                FieldInfo eulerField = typeof(XRInteractionSimulator).GetField("m_CenterEyeEuler", flags);
                FieldInfo stateField = typeof(XRInteractionSimulator).GetField("m_HMDState", flags);
                if (eulerField == null || stateField == null)
                {
                    XiloLog.Warn(LogCategory, "Simulator head fields not found, starting level", this);
                    return;
                }

                var euler = (Vector3)eulerField.GetValue(simulator);
                euler.x = rig.AuthoredPitch;
                eulerField.SetValue(simulator, euler);

                var state = (XRSimulatedHMDState)stateField.GetValue(simulator);
                state.centerEyeRotation = Quaternion.Euler(euler);
                state.deviceRotation = state.centerEyeRotation;
                stateField.SetValue(simulator, state);

                var tilt = Quaternion.Euler(rig.AuthoredPitch, 0f, 0f);
                TiltController(simulator, "m_LeftControllerState", "m_LeftControllerEuler", tilt, state.centerEyePosition, flags);
                TiltController(simulator, "m_RightControllerState", "m_RightControllerEuler", tilt, state.centerEyePosition, flags);
                XiloLog.Info(LogCategory, $"Simulated head and controllers start looking down {rig.AuthoredPitch:0.#}°", this);
            }
            catch (Exception exception)
            {
                XiloLog.Exception(LogCategory, exception, this);
            }
        }

        static bool AreSimulatedControllersTracked()
        {
            var controllers = InputSystem.devices.OfType<XRController>().ToList();
            return controllers.Any(c => c.usages.Contains(CommonUsages.LeftHand) && c.isTracked.isPressed)
                && controllers.Any(c => c.usages.Contains(CommonUsages.RightHand) && c.isTracked.isPressed);
        }

        // Swings a simulated controller around the head by the start tilt, so it stays in front of the view and aims at the desk.
        // Only a controller still in its default (level) pose is tilted, so applying twice never tilts it twice.
        void TiltController(XRInteractionSimulator simulator, string stateName, string eulerName, Quaternion tilt, Vector3 head, BindingFlags flags)
        {
            FieldInfo stateField = typeof(XRInteractionSimulator).GetField(stateName, flags);
            FieldInfo eulerField = typeof(XRInteractionSimulator).GetField(eulerName, flags);
            if (stateField == null || eulerField == null)
            {
                XiloLog.Warn(LogCategory, $"Simulator field {stateName} not found, controller starts level", this);
                return;
            }

            var controller = (XRSimulatedControllerState)stateField.GetValue(simulator);
            if (Quaternion.Angle(controller.deviceRotation, Quaternion.identity) > 0.5f)
                return;
            controller.devicePosition = head + tilt * (controller.devicePosition - head);
            controller.deviceRotation = tilt * controller.deviceRotation;
            stateField.SetValue(simulator, controller);
            eulerField.SetValue(simulator, controller.deviceRotation.eulerAngles);
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
