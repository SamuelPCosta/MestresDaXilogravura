using System;
using System.Collections.Generic;
using UnityEngine;
using Xilo.Core;

namespace Xilo.Painting
{
    [Serializable]
    public sealed class MaskBinding
    {
        public string property;
        public RenderTexture mask;
        public bool ownsMask = true;
        public bool createAtRuntime;
        public int runtimeSize = 512;
    }

    // A surface that exposes its masks to the painter and binds them to its material instance.
    // Masks may also be shared with another surface (e.g. the paper reads the matrix masks).
    public sealed class PaintableSurface : MonoBehaviour
    {
        const string LogCategory = "Surface";

        static readonly Dictionary<Collider, PaintableSurface> SurfacesByCollider = new();

        [SerializeField] SurfaceId id = SurfaceId.None;
        [SerializeField] Renderer targetRenderer;
        [SerializeField] MeshCollider meshCollider;
        [SerializeField] List<MaskBinding> masks = new();

        Material materialInstance;

        public SurfaceId Id => id;
        public Material Material => materialInstance;

        public static bool TryGet(Collider collider, out PaintableSurface surface) =>
            SurfacesByCollider.TryGetValue(collider, out surface);

        void Awake()
        {
            if (targetRenderer == null)
                targetRenderer = GetComponent<Renderer>();
            if (meshCollider == null)
                meshCollider = GetComponent<MeshCollider>();

            if (targetRenderer == null || meshCollider == null)
            {
                XiloLog.Error(LogCategory, $"{name} needs a Renderer and a MeshCollider", this);
                enabled = false;
                return;
            }

            materialInstance = targetRenderer.material;
            BindMasks();
            ClearOwnedMasks();
        }

        void OnEnable()
        {
            if (meshCollider != null)
                SurfacesByCollider[meshCollider] = this;
        }

        void OnDisable()
        {
            if (meshCollider != null)
                SurfacesByCollider.Remove(meshCollider);
        }

        void OnDestroy()
        {
            foreach (MaskBinding binding in masks)
            {
                if (binding.createAtRuntime && binding.mask != null)
                    binding.mask.Release();
            }
            if (materialInstance != null)
                Destroy(materialInstance);
        }

        public RenderTexture GetMask(string property)
        {
            foreach (MaskBinding binding in masks)
            {
                if (binding.property == property)
                    return binding.mask;
            }
            return null;
        }

        public void ClearOwnedMasks()
        {
            foreach (MaskBinding binding in masks)
            {
                if (binding.ownsMask)
                    ClearTexture(binding.mask);
            }
        }

        public void SetTexture(string property, Texture texture)
        {
            if (materialInstance != null)
                materialInstance.SetTexture(property, texture);
        }

        public void SetFloat(string property, float value)
        {
            if (materialInstance != null)
                materialInstance.SetFloat(property, value);
        }

        // Creates runtime masks when needed and assigns every mask to its exposed material property.
        void BindMasks()
        {
            foreach (MaskBinding binding in masks)
            {
                if (binding.createAtRuntime && binding.mask == null)
                {
                    binding.mask = new RenderTexture(binding.runtimeSize, binding.runtimeSize, 0, RenderTextureFormat.ARGB32)
                    {
                        name = $"{name}_{binding.property}"
                    };
                    binding.mask.Create();
                }

                if (binding.mask == null)
                {
                    XiloLog.Warn(LogCategory, $"{name}: mask {binding.property} not assigned", this);
                    continue;
                }

                materialInstance.SetTexture(binding.property, binding.mask);
            }
        }

        static void ClearTexture(RenderTexture texture)
        {
            if (texture == null)
                return;

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = texture;
            GL.Clear(true, true, Color.clear);
            RenderTexture.active = previous;
        }
    }
}
