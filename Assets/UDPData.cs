using UnityEngine;
using System;
using System.Collections.Generic;

[System.Serializable]
public class UDPData

{
    public string led;
    public int id;
    public List<float> position;
    public List<float> rotation;
    public string frame;
}