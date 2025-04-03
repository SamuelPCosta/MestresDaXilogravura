using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TrackingBtnController : MonoBehaviour
{
    [SerializeField] private UDPReceiver tracking;
    public Collider2D spriteCollider;
    private bool canClick = true;
    private bool isClickReseted = false;
    private Button selectedBtn = null;
    private Slider selectedSlider = null;

    void Update()
    {
        if (isClickReseted)
        {
            isClickReseted = false;
            canClick = true;
        }
        if (!canClick)
            return;

        Vector3 spritePosition = spriteCollider.transform.position;
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(spritePosition);

        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = new Vector2(screenPosition.x, screenPosition.y)
        };

        var results = new System.Collections.Generic.List<RaycastResult>();

        EventSystem.current.RaycastAll(pointerEventData, results);

        bool foundButton = false;
        bool foundSlider = false;
        foreach (var result in results)
        {
            //Debug.Log("Detectado: " + result.gameObject.name);
            if (result.gameObject.GetComponent<Button>() != null)
            {
                foundButton = true;
                //Debug.Log("Sprite está sobre um botão: " + result.gameObject.name);
                Button button = result.gameObject.GetComponent<Button>();
                ColorBlock colors = button.colors;
                button.targetGraphic.color = colors.pressedColor;

                if (button != selectedBtn){
                    if (selectedBtn != null)
                        selectedBtn.targetGraphic.color = colors.normalColor;
                    selectedBtn = button;
                }

                if (tracking.ledStatus) { 
                    button = result.gameObject.GetComponent<Button>();
                    ExecuteEvents.Execute(button.gameObject, pointerEventData, ExecuteEvents.pointerClickHandler);
                    StartCoroutine(WaitForNextClick());
                }
                break;
            }
            else if (result.gameObject.GetComponentInParent<Slider>() != null)
            {
                foundSlider = true;

                Slider slider = result.gameObject.GetComponentInParent<Slider>();
                selectedSlider = slider;
                //Debug.Log($"Slider detectado: {slider.name}");

                RectTransform sliderRect = slider.GetComponent<RectTransform>();

                selectedSlider.targetGraphic.color = slider.colors.pressedColor;

                if (!tracking.ledStatus)
                    continue;

                Canvas canvas = slider.GetComponentInParent<Canvas>();
                Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main;

                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    sliderRect,
                    screenPosition,
                    eventCamera,
                    out Vector2 localPoint))
                {
                    float width = sliderRect.rect.width; // Largura total do slider
                    float minX = -width * 0.5f; // Considerando o centro como referencia
                    float maxX = width * 0.5f;

                    // Normaliza corretamente
                    float normalizedValue = Mathf.InverseLerp(minX, maxX, localPoint.x);
                    slider.value = Mathf.Clamp01(normalizedValue) * (slider.maxValue - slider.minValue) + slider.minValue;
                    StartCoroutine(WaitForNextClick());

                    //Debug.Log($"localPoint.x: {localPoint.x}, Normalized: {normalizedValue}, Slider Value: {slider.value}");
                }
                break;
            }
        }

        if (!foundButton && selectedBtn != null)
                selectedBtn.targetGraphic.color = selectedBtn.colors.normalColor;

        if(!foundSlider && selectedSlider != null)
            selectedSlider.targetGraphic.color = selectedSlider.colors.normalColor;
    }

    private IEnumerator WaitForNextClick()
    {
        canClick = false;
        yield return new WaitForSeconds(1f);
        canClick = true;
    }

    private IEnumerator ResetButtonColor(Button button, Color originalColor)
    {
        yield return new WaitForSeconds(0.1f); // Tempo para parecer um clique real
        if (button != null)
        {
            button.targetGraphic.color = originalColor;
        }
    }

    public void resetClick()
    {
        isClickReseted = true;
    }

    void OnDrawGizmos()
    {
        if (spriteCollider == null) return;

        Vector3 spritePosition = spriteCollider.transform.position;
        Gizmos.color = Color.red;
        Vector3 rayDirection = spritePosition + new Vector3(0f, 0f, 1f);
        Gizmos.DrawLine(spritePosition, rayDirection);
    }
}
