using System.Collections;
using UnityEngine;
using Xilo.Core;

namespace Xilo.Tools
{
    // Sends a released tool back to its home pose on the desk.
    // The desk is tilted, so tools stay kinematic at home instead of resting with physics.
    [RequireComponent(typeof(XiloTool))]
    public sealed class ToolHomeReturn : MonoBehaviour
    {
        const string LogCategory = "Tool";

        [SerializeField] bool returnOnRelease = true;
        [SerializeField] float returnDelay = 0.15f;
        [SerializeField] float returnDuration = 0.3f;

        XiloTool tool;
        Rigidbody body;
        Transform homeParent;
        Vector3 homeLocalPosition;
        Quaternion homeLocalRotation;
        Coroutine returning;

        void Awake()
        {
            tool = GetComponent<XiloTool>();
            body = GetComponent<Rigidbody>();
            homeParent = transform.parent;
            homeLocalPosition = transform.localPosition;
            homeLocalRotation = transform.localRotation;
        }

        void OnEnable()
        {
            tool.Grabbed += HandleGrabbed;
            tool.Released += HandleReleased;
        }

        void OnDisable()
        {
            tool.Grabbed -= HandleGrabbed;
            tool.Released -= HandleReleased;
        }

        // Places the tool at home immediately (used by Restart).
        public void SnapHome()
        {
            StopReturning();
            ApplyPose(HomePosition, HomeRotation);
        }

        Vector3 HomePosition => homeParent != null ? homeParent.TransformPoint(homeLocalPosition) : homeLocalPosition;
        Quaternion HomeRotation => homeParent != null ? homeParent.rotation * homeLocalRotation : homeLocalRotation;

        void HandleGrabbed(XiloTool _)
        {
            StopReturning();
        }

        void HandleReleased(XiloTool _)
        {
            if (returnOnRelease && isActiveAndEnabled)
                returning = StartCoroutine(ReturnRoutine());
        }

        // Waits a moment, then glides the tool back to its home pose.
        IEnumerator ReturnRoutine()
        {
            yield return new WaitForSeconds(returnDelay);

            Vector3 startPosition = transform.position;
            Quaternion startRotation = transform.rotation;
            float elapsed = 0f;
            while (elapsed < returnDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / returnDuration);
                ApplyPose(Vector3.Lerp(startPosition, HomePosition, t), Quaternion.Slerp(startRotation, HomeRotation, t));
                yield return null;
            }

            ApplyPose(HomePosition, HomeRotation);
            returning = null;
            XiloLog.Info(LogCategory, $"Tool returned home: {tool.Id}", this);
        }

        void ApplyPose(Vector3 position, Quaternion rotation)
        {
            if (body != null)
            {
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
                body.position = position;
                body.rotation = rotation;
            }

            transform.SetPositionAndRotation(position, rotation);
        }

        void StopReturning()
        {
            if (returning == null)
                return;

            StopCoroutine(returning);
            returning = null;
        }
    }
}
