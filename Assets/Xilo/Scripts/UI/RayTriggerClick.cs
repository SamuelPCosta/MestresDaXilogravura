using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Xilo.UI
{
    // Makes a 3D object clickable like the UI buttons: pointing the ray at any collider under it and pulling the
    // trigger raises Clicked. While not Clickable the interactable is off, so the object never catches the ray.
    [RequireComponent(typeof(XRSimpleInteractable))]
    public sealed class RayTriggerClick : MonoBehaviour
    {
        readonly List<XRBaseInputInteractor> hovering = new();
        XRSimpleInteractable interactable;

        public event Action Clicked;

        public bool Clickable
        {
            get => Interactable.enabled;
            set
            {
                Interactable.enabled = value;
                if (!value)
                    hovering.Clear();
            }
        }

        XRSimpleInteractable Interactable => interactable != null ? interactable : interactable = GetComponent<XRSimpleInteractable>();

        void OnEnable()
        {
            Interactable.hoverEntered.AddListener(HandleHoverEntered);
            Interactable.hoverExited.AddListener(HandleHoverExited);
        }

        void OnDisable()
        {
            Interactable.hoverEntered.RemoveListener(HandleHoverEntered);
            Interactable.hoverExited.RemoveListener(HandleHoverExited);
            hovering.Clear();
        }

        void HandleHoverEntered(HoverEnterEventArgs args)
        {
            if (args.interactorObject is XRBaseInputInteractor interactor && !hovering.Contains(interactor))
                hovering.Add(interactor);
        }

        void HandleHoverExited(HoverExitEventArgs args)
        {
            if (args.interactorObject is XRBaseInputInteractor interactor)
                hovering.Remove(interactor);
        }

        // A trigger press (activate input) of any controller pointing at the object is a click, and so is a left mouse
        // click on it (desktop testing).
        void Update()
        {
            if (!Interactable.enabled || !(TriggerPressedThisFrame() || MouseClickedThisFrame()))
                return;
            Clicked?.Invoke();
        }

        // The nearest solid collider under the cursor (or a trigger of this object) must belong to this object.
        bool MouseClickedThisFrame()
        {
            Mouse mouse = Mouse.current;
            Camera view = Camera.main;
            if (mouse == null || view == null || !mouse.leftButton.wasPressedThisFrame)
                return false;

            Ray ray = view.ScreenPointToRay(mouse.position.ReadValue());
            RaycastHit[] hits = Physics.RaycastAll(ray, view.farClipPlane, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                bool mine = hit.collider.transform.IsChildOf(transform);
                if (mine || !hit.collider.isTrigger)
                    return mine;
            }
            return false;
        }

        bool TriggerPressedThisFrame()
        {
            foreach (XRBaseInputInteractor interactor in hovering)
            {
                if (interactor != null && interactor.activateInput.ReadWasPerformedThisFrame())
                    return true;
            }
            return false;
        }
    }
}
