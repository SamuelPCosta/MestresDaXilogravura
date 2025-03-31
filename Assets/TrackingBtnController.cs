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

    void Update()
    {
        if(!canClick)
            return;

        Vector3 spritePosition = spriteCollider.transform.position;
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(spritePosition);

        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = new Vector2(screenPosition.x, screenPosition.y)
        };

        var results = new System.Collections.Generic.List<RaycastResult>();

        EventSystem.current.RaycastAll(pointerEventData, results);

        foreach (var result in results)
        {
            if (result.gameObject.GetComponent<Button>() != null)
            {
                //Debug.Log("Sprite está sobre um botão: " + result.gameObject.name);
                if (tracking.ledStatus) { 
                    Button button = result.gameObject.GetComponent<Button>();
                    ExecuteEvents.Execute(button.gameObject, pointerEventData, ExecuteEvents.pointerClickHandler);
                    StartCoroutine(WaitForNextClick());
                }
                break;
            }
        }
    }

    private IEnumerator WaitForNextClick()
    {
        canClick = false;
        yield return new WaitForSeconds(1.5f);
        canClick = true;
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
