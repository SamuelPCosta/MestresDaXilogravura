using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Xilo.Core;

namespace Xilo.UI
{
    // While a ray (or the mouse) points at a button that is still disabled, its normal content is swapped for a hint
    // (e.g. Next before the step reaches its minimum progress: "Progrida mais na etapa atual!"). Pointing away, or the
    // button becoming enabled, brings the normal content back.
    [RequireComponent(typeof(Button))]
    public sealed class DisabledButtonHint : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        const string LogCategory = "ButtonHint";

        [SerializeField] GameObject normalContent;
        [SerializeField] GameObject hintContent;

        readonly Dictionary<int, PointerEventData> pointers = new();
        readonly List<int> gone = new();
        Button button;

        void Awake()
        {
            button = GetComponent<Button>();
            if (normalContent == null || hintContent == null)
            {
                XiloLog.Warn(LogCategory, $"{name}: normal or hint content not assigned", this);
                enabled = false;
                return;
            }

            // The hint must not catch the pointer itself, or swapping it in under the ray would count as leaving.
            foreach (Graphic graphic in hintContent.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
            Show(false);
        }

        void OnDisable()
        {
            pointers.Clear();
            Show(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointers[eventData.pointerId] = eventData;
            Refresh();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointers.Remove(eventData.pointerId);
            Refresh();
        }

        // Exit events can be missed (e.g. the simulator locks the mouse cursor to turn the view), so every frame each
        // pointer is checked against what it really points at now. Also catches the button unlocking while pointed at.
        void Update()
        {
            if (pointers.Count == 0)
                return;

            gone.Clear();
            foreach (KeyValuePair<int, PointerEventData> pointer in pointers)
            {
                if (!PointsAtButton(pointer.Value))
                    gone.Add(pointer.Key);
            }
            foreach (int id in gone)
                pointers.Remove(id);
            Refresh();
        }

        bool PointsAtButton(PointerEventData pointer)
        {
            GameObject target = pointer.pointerCurrentRaycast.gameObject;
            return target != null && target.transform.IsChildOf(transform);
        }

        void Refresh() => Show(pointers.Count > 0 && !button.interactable);

        void Show(bool hint)
        {
            if (normalContent == null || hintContent == null || hintContent.activeSelf == hint)
                return;
            hintContent.SetActive(hint);
            normalContent.SetActive(!hint);
        }
    }
}
