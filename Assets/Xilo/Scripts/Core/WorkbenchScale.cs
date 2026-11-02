using UnityEngine;

namespace Xilo.Core
{
    // Size of the workbench (the Desk root) relative to the scale its meter values were tuned at. Distances and
    // speeds tuned in meters (brush sizes, tip contact, lifts and hops, gesture speeds) are multiplied by Factor,
    // so rescaling the Desk keeps them in proportion with the matrix and the tools. Factor is 1 without one.
    [DefaultExecutionOrder(-1000)]
    public sealed class WorkbenchScale : MonoBehaviour
    {
        const string LogCategory = "Workbench";

        [Tooltip("Desk scale at which the meter values (brushes, contact, lifts, speeds) were tuned.")]
        [SerializeField, Min(0.001f)] float tunedAtScale = 1f;

        static WorkbenchScale current;
        static float playFactor = 1f;

        // Read once on Play; in the Editor (gizmos, previews) it follows the Desk as it is edited.
        public static float Factor
        {
            get
            {
                if (Application.isPlaying)
                    return playFactor;
                WorkbenchScale scale = current != null ? current : FindFirstObjectByType<WorkbenchScale>();
                return scale != null ? scale.Measure() : 1f;
            }
        }

        // A value tuned in meters at the reference scale, in meters at the current Desk scale.
        public static float Meters(float tuned) => tuned * Factor;

        float Measure() => transform.lossyScale.x / tunedAtScale;

        void Awake()
        {
            current = this;
            playFactor = Measure();
            XiloLog.Info(LogCategory, $"Workbench scale {transform.lossyScale.x:0.###} (tuned at {tunedAtScale:0.###}): meter values x{playFactor:0.###}", this);
        }

        void OnDestroy()
        {
            if (current != this)
                return;
            current = null;
            playFactor = 1f;
        }
    }
}
