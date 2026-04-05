using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class GlassController : InteractiveObject
{
    private RaycastHit? hit;

    public Painter painter;
    public GrabController grabController;
    public XiloController xiloController;

    public GameObject tinta;
    public GameObject roloDeTinta;
    public ParticleSystem tintaDerramada;

    private bool isInkEnable = false;

    private Dictionary<string, RenderTexture> textureDictionary = new Dictionary<string, RenderTexture>();
    private string[] textureNames = { "InkMask" };

    void Start()
    {
        currentMaterial = GetComponent<MeshRenderer>().materials[0];
        setTextures();
        painter.desligarParticulas(tintaDerramada);
    }

    public void resetTextures()
    {
        for (int i = 0; i < textureNames.Length; i++)
        {
            Graphics.SetRenderTarget(textureDictionary[textureNames[i]]);
            GL.Clear(true, true, Color.black);
        }
    }

    public void setTextures()
    {
        textureDictionary.Clear();
        for (int i = 0; i < textureNames.Length; i++)
        {
            textureDictionary[textureNames[i]] = new RenderTexture(dimensions[0], dimensions[1], 0, RenderTextureFormat.ARGBFloat);
            Graphics.SetRenderTarget(textureDictionary[textureNames[i]]);
            GL.Clear(true, true, Color.black);
            Graphics.SetRenderTarget(null);
            currentMaterial.SetTexture(textureNames[i], textureDictionary[textureNames[i]]);
        }
    } 

    public void Draw()
    {
        int layerMask = 1 << LayerMask.NameToLayer("glass");
        GameObject tool = null;

        if ((hit = painter.CheckDraw(tinta, layerMask, xiloController.isSanded, false, tintaDerramada)) != null)
        {
            painter.SetBrushPreset(Brush.Ink);
            isInkEnable = true;
            tool = tinta;
        }
        else
        if (painter.CheckDraw(roloDeTinta, layerMask, isInkEnable, false, null) != null)
        {
            roloDeTinta.GetComponent<InkRollerController>().enableInk();
            tool = roloDeTinta;
        }

        if (hit != null) {
            RaycastHit validHit = hit.Value;
            painter.PaintMask(textureDictionary["InkMask"], validHit, false);
            tool.GetComponent<Tool>().initSound();
            if (validHit.collider == null || (painter.mode.mode == Mode.VR && grabController.isToolNull()))
                painter.desligarParticulas(tintaDerramada);
        }
    }

    void Update()
    {
        Draw();
    }

    public void resetValues()
    {
        isInkEnable = false;
        roloDeTinta.GetComponent<InkRollerController>().resetValues();
    }

    public bool getInkEnable()
    {
        return isInkEnable;
    }
}
