using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Xilo.Core;

namespace Xilo.Tools
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public sealed class XiloTool : MonoBehaviour
    {
        const string LogCategory = "Tool";

        [SerializeField] ToolId id = ToolId.None;
        [SerializeField] Transform tip;
        [SerializeField] AudioSource loopAudio;
        [SerializeField] ParticleSystem contactParticles;

        [Header("Haptics")]
        [SerializeField, Range(0f, 1f)] float hapticAmplitude = 0.15f;
        [SerializeField] float hapticDuration = 0.05f;

        XRGrabInteractable grab;
        IXRSelectInteractor holder;
        bool isUsing;

        public event Action<XiloTool> Grabbed;
        public event Action<XiloTool> Released;

        public ToolId Id => id;
        public Transform Tip => tip != null ? tip : transform;
        public bool IsHeld => holder != null;
        public bool IsUsing => isUsing;
        public bool IsActiveForStep { get; set; }
        public XRGrabInteractable Grab => grab;

        void Awake()
        {
            grab = GetComponent<XRGrabInteractable>();
            if (tip == null)
                XiloLog.Warn(LogCategory, $"{name} has no tip assigned, using its own transform", this);
        }

        void OnEnable()
        {
            grab.selectEntered.AddListener(HandleSelectEntered);
            grab.selectExited.AddListener(HandleSelectExited);
        }

        void OnDisable()
        {
            grab.selectEntered.RemoveListener(HandleSelectEntered);
            grab.selectExited.RemoveListener(HandleSelectExited);
            EndUse();
        }

        // Starts or keeps the usage feedback (sound, particles, haptics) at the contact point.
        public void BeginUse(Vector3 point)
        {
            if (contactParticles != null)
            {
                contactParticles.transform.position = point;
                if (!contactParticles.isPlaying)
                {
                    contactParticles.gameObject.SetActive(true);
                    contactParticles.Play();
                }
            }

            SendHaptic();

            if (isUsing)
                return;

            isUsing = true;
            if (loopAudio != null && !loopAudio.isPlaying)
                loopAudio.Play();
        }

        // Stops every usage feedback.
        public void EndUse()
        {
            if (!isUsing)
                return;

            isUsing = false;
            if (loopAudio != null)
                loopAudio.Stop();
            if (contactParticles != null)
                contactParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        void SendHaptic()
        {
            if (hapticAmplitude <= 0f || holder is not XRBaseInputInteractor inputInteractor)
                return;

            inputInteractor.SendHapticImpulse(hapticAmplitude, hapticDuration);
        }

        void HandleSelectEntered(SelectEnterEventArgs args)
        {
            holder = args.interactorObject;
            XiloLog.Info(LogCategory, $"Tool grabbed: {id}", this);
            Grabbed?.Invoke(this);
        }

        void HandleSelectExited(SelectExitEventArgs args)
        {
            holder = null;
            EndUse();
            XiloLog.Info(LogCategory, $"Tool released: {id}", this);
            Released?.Invoke(this);
        }
    }
}
