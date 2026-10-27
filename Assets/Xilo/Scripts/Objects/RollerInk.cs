using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Xilo.Core;

namespace Xilo.Objects
{
    // Ink state of the roller rubber (port of the legacy InkRollerController).
    // Drives the existing Borracha shader through a runtime material instance.
    public sealed class RollerInk : MonoBehaviour
    {
        const string LogCategory = "Roller";

        [SerializeField] Renderer rubberRenderer;
        [SerializeField] float loadDuration = 1.25f;

        [Header("Hooks")]
        public UnityEvent OnRollerLoaded = new();

        Material rubberMaterial;
        Coroutine loading;

        public bool IsLoaded { get; private set; }
        public bool IsLoading => loading != null;

        public event Action Loaded;

        void Awake()
        {
            if (rubberRenderer == null)
            {
                XiloLog.Error(LogCategory, "Rubber renderer not assigned", this);
                enabled = false;
                return;
            }

            rubberMaterial = rubberRenderer.material;
            ApplyEmpty();
        }

        void OnDestroy()
        {
            if (rubberMaterial != null)
                Destroy(rubberMaterial);
        }

        // Starts loading the rubber with ink; ignored if already loaded or loading.
        public void Load()
        {
            if (IsLoaded || loading != null || rubberMaterial == null)
                return;

            XiloLog.Info(LogCategory, "Loading roller with ink", this);
            loading = StartCoroutine(LoadRoutine());
        }

        public void ResetInk()
        {
            if (loading != null)
            {
                StopCoroutine(loading);
                loading = null;
            }
            IsLoaded = false;
            ApplyEmpty();
        }

        // Fades the ink level from 1 to 0 like the legacy controller, then marks the roller as loaded.
        IEnumerator LoadRoutine()
        {
            rubberMaterial.SetFloat(ShaderProps.RollerHasInkId, 1f);
            float elapsed = 0f;
            while (elapsed < loadDuration)
            {
                elapsed += Time.deltaTime;
                rubberMaterial.SetFloat(ShaderProps.RollerInkLevelId, Mathf.Lerp(1f, 0f, elapsed / loadDuration));
                yield return null;
            }

            rubberMaterial.SetFloat(ShaderProps.RollerInkLevelId, 0f);
            loading = null;
            IsLoaded = true;
            XiloLog.Info(LogCategory, "Roller loaded", this);
            Loaded?.Invoke();
            OnRollerLoaded.Invoke();
        }

        void ApplyEmpty()
        {
            if (rubberMaterial == null)
                return;
            rubberMaterial.SetFloat(ShaderProps.RollerHasInkId, 0f);
            rubberMaterial.SetFloat(ShaderProps.RollerInkLevelId, 1f);
        }
    }
}
