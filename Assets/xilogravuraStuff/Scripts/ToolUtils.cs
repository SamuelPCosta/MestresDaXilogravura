using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolUtils : MonoBehaviour
{
    public bool isPlaying = false;

    public void initSound()
    {
        if (!isPlaying)
        {
            isPlaying = true;
            GetComponent<AudioSource>().Play();
        }
    }

    public void stopSound()
    {
        if (GetComponent<AudioSource>() != null)
        {
            GetComponent<AudioSource>().Stop();
            isPlaying = false;
        }
    }
}
