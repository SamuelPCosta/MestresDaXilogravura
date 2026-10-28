using UnityEngine;
using Xilo.Core;

namespace Xilo.Objects
{
    // The printing sheet: owns its motion (PoseTween) and its sounds (placing on the matrix, turning to reveal).
    [RequireComponent(typeof(PoseTween), typeof(AudioSource))]
    public sealed class PaperSheet : MonoBehaviour
    {
        const string LogCategory = "Paper";

        [SerializeField] AudioClip placeClip;
        [SerializeField] AudioClip revealClip;

        [Header("Reveal")]
        [SerializeField, Tooltip("Turn axis in the sheet's local space (an in-plane axis turns the sheet over).")]
        Vector3 flipLocalAxis = Vector3.up;
        [SerializeField, Tooltip("Signed: the sign sets the turn direction.")] float flipDegrees = -180f;
        [SerializeField, Tooltip("Peak lift (meters) along the sheet normal halfway through, so it clears the matrix.")]
        float flipLift = 0.6f;
        [SerializeField] float flipDuration = 1.2f;

        PoseTween tween;
        AudioSource audioSource;

        public PoseTween Tween => tween != null ? tween : tween = GetComponent<PoseTween>();

        // Turns the sheet over in place (legacy "resultado" motion) with its sound.
        public void Reveal(System.Action onCompleted = null)
        {
            PlayRevealSound();
            Tween.Play(FlipPose(), flipDuration, onCompleted);
        }

        // Rotation around the in-plane axis, lifted along the normal that points away from the desk.
        System.Func<float, (Vector3, Quaternion)> FlipPose()
        {
            Vector3 normal = Normal;
            if (Vector3.Dot(normal, Vector3.up) < 0f)
                normal = -normal;
            return Tween.SpinPose(transform.TransformDirection(flipLocalAxis), flipDegrees, normal * flipLift);
        }

#if UNITY_EDITOR
        [ContextMenu("Preview reveal (and back)")]
        public void PreviewReveal() => Tween.Preview(FlipPose(), flipDuration, returns: true);
#endif

        void Awake()
        {
            tween = GetComponent<PoseTween>();
            audioSource = GetComponent<AudioSource>();
        }

        public void PlayPlaceSound() => Play(placeClip);

        public void PlayRevealSound() => Play(revealClip);

        // The sheet is a flat mesh: its thinnest local bounds axis is the normal, whatever the import orientation.
        public Vector3 Normal
        {
            get
            {
                Mesh mesh = TryGetComponent(out MeshFilter filter) ? filter.sharedMesh : null;
                if (mesh == null)
                    return transform.forward;
                Vector3 size = mesh.bounds.size;
                Vector3 axis = size.x <= size.y && size.x <= size.z ? Vector3.right : size.y <= size.z ? Vector3.up : Vector3.forward;
                return transform.TransformDirection(axis);
            }
        }

        void Play(AudioClip clip)
        {
            if (clip == null || audioSource == null)
            {
                XiloLog.Warn(LogCategory, "Sound clip or AudioSource missing", this);
                return;
            }
            audioSource.PlayOneShot(clip);
        }
    }
}
