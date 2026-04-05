using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class XRKnob : XRBaseInteractable
{
    public Transform knobTransform = null;

    [Range(-180, 0)] public float minimum = -90.0f;
    [Range(0, 180)] public float maximum = 90.0f;
    [Range(0, 1)] public float defaultValue = 0.0f;

    [Serializable] public class ValueChangeEvent : UnityEvent<float> { }
    public ValueChangeEvent OnValueChange = new ValueChangeEvent();

    public float Value { get; private set; } = 0.0f;
    public float Angle { get; private set; } = 0.0f;

    private IXRSelectInteractor selectInteractor = null;
    private Quaternion selectRotation = Quaternion.identity;

    private void Start()
    {
        float defaultRotation = Mathf.Lerp(minimum, maximum, defaultValue);
        ApplyRotation(defaultRotation);
        SetValue(defaultRotation);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        selectEntered.AddListener(StartTurn);
        selectExited.AddListener(EndTurn);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        selectEntered.RemoveListener(StartTurn);
        selectExited.RemoveListener(EndTurn);
    }

    private void StartTurn(SelectEnterEventArgs eventArgs)
    {
        selectInteractor = eventArgs.interactorObject;
        selectRotation = selectInteractor.transform.rotation;
    }

    private void EndTurn(SelectExitEventArgs eventArgs)
    {
        selectInteractor = null;
        selectRotation = Quaternion.identity;
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);

        if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
        {
            if (selectInteractor != null)
            {
                Angle = FindRotationValue();
                float finalRotation = ApplyRotation(Angle);

                SetValue(finalRotation);
                selectRotation = selectInteractor.transform.rotation;
            }
        }
    }

    private float FindRotationValue()
    {
        Quaternion rotationDifference = selectInteractor.transform.rotation * Quaternion.Inverse(selectRotation);
        Vector3 rotatedForward = rotationDifference * knobTransform.forward;
        return (Vector3.SignedAngle(knobTransform.forward, rotatedForward, transform.up));
    }

    private float ApplyRotation(float angle)
    {
        Quaternion newRotation = Quaternion.AngleAxis(angle, Vector3.up);
        newRotation *= knobTransform.localRotation;

        Vector3 eulerRotation = newRotation.eulerAngles;

        float yAngle = eulerRotation.y;
        if (yAngle > 180) yAngle -= 360;

        yAngle = Mathf.Clamp(yAngle, minimum, maximum);
        eulerRotation.y = yAngle;

        knobTransform.localEulerAngles = eulerRotation;
        return yAngle;
    }

    private void SetValue(float rotation)
    {
        Value = Mathf.InverseLerp(minimum, maximum, rotation);
        OnValueChange.Invoke(Value);
    }
}