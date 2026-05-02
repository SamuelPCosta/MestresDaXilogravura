using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// This script either forces the selection or deselection of an interactable objects by the interactor this script is on.
/// </summary>

public class ManuallySelectObject : MonoBehaviour
{
    [Tooltip("What object are we selecting?")]
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable interactable = null;

    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor interactor = null;
    private XRInteractionManager interactionManager = null;

    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor.InputTriggerType originalTriggerType;

    private void Awake()
    {
        interactor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor>();
        interactionManager = interactor.interactionManager;
        originalTriggerType = interactor.selectActionTrigger;
    }

    public XRInteractionManager GetInteractionManager()
    {
        return interactionManager;
    }

    public void ManuallySelect(XRInteractionManager interactionManager)
    {
        interactable.gameObject.SetActive(true);
        interactor.selectActionTrigger = UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor.InputTriggerType.StateChange;
        interactionManager.SelectEnter((IXRSelectInteractor)interactor, (IXRSelectInteractable)interactable);
    }

    public void ManuallyDeselect()
    {
        interactionManager.CancelInteractorSelection((IXRSelectInteractor)interactor);
        interactor.selectActionTrigger = originalTriggerType;
        interactable.gameObject.SetActive(false);
    }
}
