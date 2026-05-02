using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToolsController : MonoBehaviour
{
    public int paintAngle = 350;
    public UDPReceiver tracking;
    private Transform tool = null;

    public bool condicaoDePintura()
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
        if(tool != null && tool.GetComponent<Tool>() != null)
            tool.GetComponent<Tool>().stopSound();
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
