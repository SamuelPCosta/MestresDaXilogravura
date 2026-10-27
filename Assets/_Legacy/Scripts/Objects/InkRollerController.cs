using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InkRollerController : MonoBehaviour
{
    private bool isInk = false;
    private bool tintaNoRolinho = false;
    public Material borracha;
    // Start is called before the first frame update
    void Start()
    {
        borracha.SetFloat("isTinta", 0);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void enableInk()
    {
        isInk = true;
        borracha.SetFloat("isTinta", 1);
        if (!tintaNoRolinho)
        {
            borracha.SetFloat("inkLevel", 1);
            StartCoroutine(DecreaseInkLevelOverTime());
        }
    }

    public bool isInkEnable()
    {
        return isInk;
    }

    IEnumerator DecreaseInkLevelOverTime()
    {
        float currentTime = 0f;
        float startInkLevel = 1f;
        float endInkLevel = 0f;
        float duration = 1.25f;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            float newInkLevel = Mathf.Lerp(startInkLevel, endInkLevel, currentTime / duration);
            borracha.SetFloat("inkLevel", newInkLevel);
            yield return null;
        }

        borracha.SetFloat("inkLevel", endInkLevel);
        tintaNoRolinho = true;
    }

    public void resetValues()
    {
        tintaNoRolinho = false;
        isInk = false;
        borracha.SetFloat("inkLevel", 1);
    }
}
