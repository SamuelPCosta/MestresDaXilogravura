using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlassController : InteractiveObject
{
    private RaycastHit? hit;
    public XiloController xiloController;

    public ParticleSystem particles;

    private bool isInkEnable = false;

    private Dictionary<string, RenderTexture> textureDictionary = new Dictionary<string, RenderTexture>();
    private string[] textureNames = { "InkMask" };

    void Start()
    {
        currentMaterial = GetComponent<MeshRenderer>().materials[0];
    }

    public void resetTextures()
    {
        for (int i = 0; i < textureNames.Length; i++)
        {
            Graphics.SetRenderTarget(textureDictionary[textureNames[i]]);
            GL.Clear(true, true, Color.black);
        }
    }

    public void resetValues()
    {
        isInkEnable = false;
        //roll.GetComponent<InkRollerController>().resetValues();
    }

    public bool getInkEnable()
    {
        return isInkEnable;
    }
}
