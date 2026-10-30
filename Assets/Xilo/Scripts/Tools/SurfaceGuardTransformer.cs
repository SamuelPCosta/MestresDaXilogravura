using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Transformers;
using Xilo.Core;

namespace Xilo.Tools
{
    // Keeps a held tool from going through SurfaceBlockers: runs right after the default grab transformer (which
    // makes the tool follow the hand) and pushes the target pose out along the surface normal, so the tool rests and
    // slides on the surface while the hand may go a bit deeper. No physics: no lag, no jitter.
    // Guards the points that actually touch (the tip and the painting probe samples), so they stop exactly on the
    // surface and keep painting; the bulkier collider would hold the real tip off the surface.
    public sealed class SurfaceGuardTransformer : XRBaseGrabTransformer
    {
        const string LogCategory = "SurfaceGuard";

        [Tooltip("Extra tool points that can't go through, besides the tip and the painting probe samples.")]
        [SerializeField] Transform[] extraPoints;

        Transform[] guardPoints = new Transform[0];

        protected override RegistrationMode registrationMode => RegistrationMode.None;

        protected override void Start()
        {
            base.Start();
            guardPoints = CollectGuardPoints();
            XiloLog.Info(LogCategory, $"{name} guards {guardPoints.Length} points: {string.Join(", ", guardPoints.Select(point => point.name))}", this);

            // The default transformer must come first so this one receives the pose that follows the hand.
            if (!TryGetComponent(out XRGrabInteractable grab))
            {
                XiloLog.Error(LogCategory, $"{name} has no XRGrabInteractable", this);
                return;
            }
            var follow = GetComponent<XRGeneralGrabTransformer>();
            if (follow == null)
                follow = gameObject.AddComponent<XRGeneralGrabTransformer>();
            if (!IsRegistered(grab, follow))
                grab.AddSingleGrabTransformer(follow);
            grab.AddSingleGrabTransformer(this);
        }

        Transform[] CollectGuardPoints()
        {
            var points = new List<Transform>();
            if (TryGetComponent(out XiloTool tool) && tool.Tip != transform)
                points.Add(tool.Tip);
            if (TryGetComponent(out ToolTipProbe probe) && probe.Samples != null)
                points.AddRange(probe.Samples);
            if (extraPoints != null)
                points.AddRange(extraPoints);
            return points.Where(point => point != null).Distinct().ToArray();
        }

        static bool IsRegistered(XRGrabInteractable grab, IXRGrabTransformer transformer)
        {
            for (int i = 0; i < grab.singleGrabTransformersCount; i++)
            {
                if (ReferenceEquals(grab.GetSingleGrabTransformerAt(i), transformer))
                    return true;
            }
            return false;
        }

        public override void Process(XRGrabInteractable grabInteractable, XRInteractionUpdateOrder.UpdatePhase updatePhase, ref Pose targetPose, ref Vector3 localScale)
        {
            Vector3 hand = targetPose.position;
            foreach (SurfaceBlocker blocker in SurfaceBlocker.Active)
            {
                if (!blocker.Guards(hand))
                    continue;

                // Lift by the deepest point, so one push puts every point on or above the surface.
                float deepest = 0f;
                foreach (Transform point in guardPoints)
                    deepest = Mathf.Max(deepest, blocker.Penetration(AtTargetPose(point.position, targetPose)));
                if (deepest > 0f)
                    targetPose.position += blocker.Normal * deepest;
            }
        }

        // Where a point of the tool would be if the tool were at the target pose.
        Vector3 AtTargetPose(Vector3 worldPoint, Pose targetPose)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint);
            return targetPose.position + targetPose.rotation * Vector3.Scale(transform.lossyScale, local);
        }
    }
}
