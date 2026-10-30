using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

namespace Xilo.UI
{
    // The XRI ray uses one color for any UI it points at; this switches it to the button color only while the UI
    // under the ray is clickable (a button, slider or other Selectable). Tools keep the hover color of the line.
    [RequireComponent(typeof(CurveVisualController))]
    public sealed class RayButtonTint : MonoBehaviour
    {
        [SerializeField] NearFarInteractor interactor;
        [SerializeField] Color buttonColor = new(0.2f, 0.9f, 0.3f, 1f);

        CurveVisualController curve;
        Gradient plainGradient;
        Gradient buttonGradient;
        bool overButton;

        void Awake()
        {
            curve = GetComponent<CurveVisualController>();
            if (interactor == null)
                interactor = GetComponentInParent<NearFarInteractor>();

            plainGradient = curve.uiHitProperties.gradient;
            buttonGradient = new Gradient { mode = plainGradient.mode };
            buttonGradient.SetKeys(new[] { new GradientColorKey(buttonColor, 0f), new GradientColorKey(buttonColor, 1f) }, plainGradient.alphaKeys);
        }

        void LateUpdate()
        {
            bool onButton = interactor != null
                && interactor.TryGetCurrentUIRaycastResult(out RaycastResult hit)
                && hit.gameObject != null
                && hit.gameObject.GetComponentInParent<Selectable>() != null;

            if (onButton == overButton)
                return;
            overButton = onButton;
            curve.uiHitProperties.gradient = onButton ? buttonGradient : plainGradient;
        }
    }
}
