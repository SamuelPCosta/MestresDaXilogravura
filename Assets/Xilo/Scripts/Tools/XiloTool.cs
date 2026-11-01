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
        [Tooltip("Peak vibration, reached at Full Speed (or always, when the feedback doesn't follow the gesture).")]
        [SerializeField, Range(0f, 1f)] float hapticAmplitude = 0.15f;
        [SerializeField] float hapticDuration = 0.05f;

        [Header("Gesture feedback")]
        [Tooltip("Sound and vibration follow the tip speed: silent when the hand rests, louder and higher as it moves faster. " +
                 "Off for tools whose use isn't a stroke (e.g. pouring ink).")]
        [SerializeField] bool followGesture = true;
        [Tooltip("Tip speed (m/s) that gives full volume and vibration.")]
        [SerializeField, Min(0.01f)] float fullSpeed = 0.25f;
        [Tooltip("Loop pitch at rest and at Full Speed.")]
        [SerializeField] Vector2 pitchRange = new(0.9f, 1.1f);
        [Tooltip("Seconds the feedback takes to follow a change of speed (and to fade out when the tool leaves the surface).")]
        [SerializeField, Range(0.01f, 0.5f)] float responseTime = 0.08f;

        XRGrabInteractable grab;
        IXRSelectInteractor holder;
        bool isUsing;
        float loopVolume = 1f;
        float level;
        float tipSpeed;
        Vector3 lastTipPosition;

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
            if (contactParticles != null)
                contactParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (loopAudio != null)
                loopVolume = loopAudio.volume;
            lastTipPosition = Tip.position;
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
            level = 0f;
            if (loopAudio != null)
                loopAudio.Stop();
        }

        // Starts or keeps the usage feedback (sound, particles, haptics) at the contact point.
        // Sound and vibration levels are driven every frame in Update.
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

            if (isUsing)
                return;

            isUsing = true;
            if (loopAudio != null && !loopAudio.isPlaying)
            {
                loopAudio.volume = loopVolume * level;
                loopAudio.Play();
            }
        }

        // Stops every usage feedback; the loop fades out over Response Time.
        public void EndUse()
        {
            if (!isUsing)
                return;

            isUsing = false;
            if (contactParticles != null)
                contactParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        void Update()
        {
            TrackTipSpeed();
            UpdateLevel();
            ApplyLoop();
            SendHaptic();
        }

        void TrackTipSpeed()
        {
            Vector3 position = Tip.position;
            if (Time.deltaTime > 0f)
                tipSpeed = Vector3.Distance(position, lastTipPosition) / Time.deltaTime;
            lastTipPosition = position;
        }

        // 0..1 feedback level: follows the tip speed while in use (or full, for tools that don't follow the gesture),
        // eases towards it so a jittery speed doesn't make the sound flutter, and falls to 0 when not in use.
        void UpdateLevel()
        {
            float target = !isUsing ? 0f : followGesture ? Mathf.Clamp01(tipSpeed / fullSpeed) : 1f;
            level = Mathf.Lerp(level, target, 1f - Mathf.Exp(-Time.deltaTime / responseTime));
            if (!isUsing && level < 0.005f)
                level = 0f;
        }

        void ApplyLoop()
        {
            if (loopAudio == null)
                return;

            if (level <= 0f)
            {
                if (loopAudio.isPlaying)
                    loopAudio.Stop();
                return;
            }

            loopAudio.volume = loopVolume * level;
            if (followGesture)
                loopAudio.pitch = Mathf.Lerp(pitchRange.x, pitchRange.y, level);
        }

        void SendHaptic()
        {
            if (!isUsing || hapticAmplitude <= 0f || level < 0.02f || holder is not XRBaseInputInteractor inputInteractor)
                return;

            inputInteractor.SendHapticImpulse(hapticAmplitude * level, Mathf.Max(hapticDuration, Time.deltaTime * 2f));
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
