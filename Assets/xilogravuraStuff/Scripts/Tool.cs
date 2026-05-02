using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AvailableTools { PENCIL, GOUGE, SANDPAPER, INK, PAINT_ROLLER, BAREN }

public class Tool : MonoBehaviour
{
    [SerializeField] private Transform pointer;
    [SerializeField] private Transform[] tools;
    [SerializeField] private ToolsController mode;
    [SerializeField] private DynamicPainting paint;

    private bool resetCursor = false;
    private Transform currentTool;

    private const int POINTER_ID = 10;
    private bool stabilize = false;

    public void checkTool(UDPData jsonData, bool isCursor)
    {
        if (jsonData.id == POINTER_ID)
        {
            pointer.gameObject.SetActive(true);
            foreach (Transform t in tools)
                t.gameObject.SetActive(false);

            currentTool = pointer;
            isCursor = true;
            if (!resetCursor)
            {
                resetCursor = true;
                pointer.GetComponent<TrackingBtnController>().resetClick();
            }
            UpdateTransform(jsonData, true);
        }
        else
        if (jsonData.id >= 0 && jsonData.id < 10)
        {
            if (jsonData.position != null && jsonData.position.Count >= 3 &&
                jsonData.rotation != null && jsonData.rotation.Count >= 3)
            {
                bool ret = UpdateTool(jsonData);
                if (ret) {
                    UpdateTransform(jsonData, false);
                    mode.setTool(tools[jsonData.id]);
                }
            }
        }
        else {
            isCursor = false;
            pointer.gameObject.SetActive(false);

            foreach (Transform t in tools)
                t.gameObject.SetActive(false);
            currentTool = null;
            mode.resetTool();
            stabilize = false;
            resetCursor = false;
        }
    }

    //ativa as ferramentas
    private bool UpdateTool(UDPData data)
    {
        for (int i = 0; i < tools.Length; i++)
        {
            tools[i].gameObject.SetActive(data.id == i);

            if (data.id == i)
            {
                currentTool = tools[i];
                paint.setPointer(currentTool.GetChild(0));
            }
        }

        if (currentTool == null)
        {
            paint.resetPointer();
            return false;
        }
        return true;
    }

    private void UpdateTransform(UDPData data, bool Movement2D){
        float influency = -0.75f;
        float influencyRoatation = 58f;
        float lerpFactor = (currentTool != null) ? 0.85f : 1f;

        Vector3 targetPosition = new Vector3(
            data.position[0] * influency,
            data.position[1] * influency,
            0f
        );

        Vector3 futurePositon = Vector3.Lerp(
            currentTool.localPosition,
            targetPosition,
            lerpFactor
        );

        currentTool.localPosition = futurePositon;

        if (Movement2D)
            return;

        Quaternion targetRotation = Quaternion.Euler(
            data.rotation[0] * influencyRoatation,
            data.rotation[1] * influencyRoatation,
            data.rotation[2] * influencyRoatation
        );

        currentTool.localRotation = Quaternion.Lerp(
            currentTool.localRotation,
            targetRotation,
            lerpFactor
        );
    }

    public GameObject getCurrentTool() {
        if (currentTool != null)
            return currentTool.gameObject;
        else return null;
    }

    public GameObject getTool(AvailableTools index) => tools[(int)index].gameObject;

    #region sound
    public bool isPlaying = false;

    public void initSound()
    {
        if (currentTool != null && !isPlaying)
        {
            isPlaying = true;
            currentTool.GetComponent<AudioSource>().Play();
        }
    }

    public void stopSound()
    {
        if (currentTool != null && currentTool.GetComponent<AudioSource>() != null)
        {
            currentTool.GetComponent<AudioSource>().Stop();
            isPlaying = false;
        }
    }
    #endregion

    #region particles
    public void turnOnParticles(ParticleSystem particulas, Vector3 point)
    {
        particulas.gameObject.SetActive(true);
        particulas.transform.position = point;
        particulas.Play();
    }

    public void turnOffParticles(ParticleSystem particulas)
    {
        particulas.Pause();
        particulas.gameObject.SetActive(false);
    }
    #endregion
}
