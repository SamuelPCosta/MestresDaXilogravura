using UnityEngine;
using UnityEngine.InputSystem;
using Xilo.Core;

namespace Xilo.Dev
{
    // Editor-only shortcuts. Function keys avoid clashes with the XR Interaction Simulator bindings.
    public sealed class DebugStepHotkeys : MonoBehaviour
    {
        const string LogCategory = "Debug";

        [SerializeField] ExperienceFlow flow;

#if UNITY_EDITOR
        void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || flow == null)
                return;

            if (keyboard.f5Key.wasPressedThisFrame)
                Run("Force next", flow.ForceNext);
            else if (keyboard.f6Key.wasPressedThisFrame)
                Run("Back", flow.Back);
            else if (keyboard.f9Key.wasPressedThisFrame)
                Run("Restart", flow.Restart);
        }

        void Run(string label, System.Action action)
        {
            XiloLog.Info(LogCategory, label, this);
            action();
        }
#endif
    }
}
