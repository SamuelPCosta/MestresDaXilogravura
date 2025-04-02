using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectionMode : ExperienceMode
{
    public int paintAngle = 350;
    public UDPReceiver tracking;
    private Transform tool = null;

    public void Start()
    {
        mode = Mode.PROJECTION;
    }

    public override void getMode()
    {
        Debug.Log("MODE: " + mode);
    }

    public override bool condicaoDePintura()
    {
        //print(tool.transform.eulerAngles.x);
        if(tool == null)
            return false;
        //return true;
        //print(tool.parent.name +": "+tool.parent.transform.localPosition.z);

        if (tool.name.Equals("colher"))
            return true;

        return tracking.ledStatus;
    }

    public void setTool(Transform tool){
        this.tool = tool;
    }

    public Transform getTool()
    {
        return tool;
    }

    public void resetTool()
    {
        if(tool != null && tool.GetComponent<ToolUtils>() != null)
            tool.GetComponent<ToolUtils>().stopSound();
        tool = null;
    }

    public bool isToolInUse()
    {
        return tool != null;
    }

    public bool checkTool(GameObject toolCheck)
    {
        if(tool != null)
            return tool.name.Equals(toolCheck.name);
        return false;
    }
}
