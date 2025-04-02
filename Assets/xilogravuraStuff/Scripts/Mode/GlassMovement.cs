using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlassMovement : MonoBehaviour
{
    public ProjectionMode projectionMode;
    public XiloController xiloController;
    public InkRollerController inkRollerController;
    private bool isGlass = false;
    private float initialPosition;

    void Start()
    {
        initialPosition = transform.localPosition.y;
    }

    void Update()
    {
        Transform tool = projectionMode.getTool();
        float startPos = transform.localPosition.y;
        if (tool != null && tool.name.Equals("tinta") && !isGlass && xiloController.getSanded())
            StartCoroutine(MoveToPosition(startPos, .85f));
        if (inkRollerController.isInkEnable() && isGlass)
            StartCoroutine(MoveToPosition(startPos, initialPosition));
    }

    IEnumerator MoveToPosition(float StartPosition, float EndPosition)
    {
        isGlass = !isGlass;
        float duration = 1f;
        float elapsedTime = 0.0f;
        Vector3 startPos = transform.localPosition;
        Vector3 endPos = new Vector3(transform.localPosition.x, EndPosition, transform.localPosition.z);
        startPos.y = StartPosition;

        while (elapsedTime < duration)
        {
            transform.localPosition = Vector3.Lerp(startPos, endPos, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = endPos;
    }
}
