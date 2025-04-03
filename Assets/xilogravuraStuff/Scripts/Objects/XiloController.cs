using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class XiloController : InteractiveObject
{
    private RaycastHit? hit;

    public Painter painter;
    public GrabController grabController;
    public PaperController paperController;

    [Header("Tools")]
    public GameObject lapisDeRascunho;
    public GameObject goiva;
    public GameObject lixa;
    public GameObject roloDeTinta;

    [Header("Particles")]
    public ParticleSystem lascasDeMadeira;
    public ParticleSystem poDeMadeira;

    [SerializeField]
    private TouchController touch;

    public bool isStart = false;

    private bool isSketched = false;
    private bool isSculped = false;
    private bool isSanded = false;
    private bool isPaint = false;

    private Dictionary<string, RenderTexture> textureDictionary = new Dictionary<string, RenderTexture>();
    private string[] textureNames = { "SketchMask", "SculptMask", "SandpaperMask", "PaintMask", "PrintMaskOld" };

    private void Awake()
    {
        //for (int i = 0; i < objectNames.Count; i++)
        //{
        //    objectNames[i]?.SetActive(false);
        //    //Debug.Log(objectNames[i].name);
        //}
    }
    void Start()
    {
        currentMaterial = GetComponent<MeshRenderer>().materials[0];
        setTextures();
    }

    public void resetTextures()
    {
        for (int i = 0; i < textureNames.Length; i++)
        {
            Graphics.SetRenderTarget(textureDictionary[textureNames[i]]);
            GL.Clear(true, true, Color.black);
        }
    }

    public void ResetOneTexture(string textureName)
    {
        Graphics.SetRenderTarget(textureDictionary[textureName]);
        GL.Clear(true, true, Color.black);
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

    public void Draw(){
        int layerMask = 1 << LayerMask.NameToLayer("wood");
        RenderTexture mask = null;
        bool interpolate = false;
        GameObject tool = null;

        if ((hit = painter.CheckDraw(lapisDeRascunho, layerMask, true, isSculped, null)) != null)
        {
            mask = textureDictionary["SketchMask"];
            painter.SetBrushPreset(Brush.HardCircle);
            marcarEtapa(ref isSketched);
            tool = lapisDeRascunho;
            //interpolate = true;
        } else if ((hit = painter.CheckDraw(goiva, layerMask, isSketched, isSanded, lascasDeMadeira)) != null)
        {
            mask = textureDictionary["SculptMask"];
            painter.SetBrushPreset(Brush.HardSquare);
            marcarEtapa(ref isSculped);
            tool = goiva;
        }
        else if ((hit = painter.CheckDraw(lixa, layerMask, isSculped, isPaint, poDeMadeira)) != null)
        {
            mask = textureDictionary["SandpaperMask"];
            painter.SetBrushPreset(Brush.SoftSquare);
            marcarEtapa(ref isSanded);
            tool = lixa;
        }
        else if (roloDeTinta.GetComponent<InkRollerController>().isInkEnable() &&
                (hit = painter.CheckDraw(roloDeTinta, layerMask, isSanded, paperController.isPrinted(), null)) != null)
        {
            mask = textureDictionary["PaintMask"];
            painter.SetBrushPreset(Brush.SoftSquare);
            marcarEtapa(ref isPaint);
            tool = roloDeTinta;
        }

        if (hit != null){
            RaycastHit validHit = hit.Value;
            if (validHit.collider == null || (painter.mode.mode == Mode.VR && grabController.isToolNull())){
                painter.desligarParticulas(lascasDeMadeira);
                painter.desligarParticulas(poDeMadeira);
            }

            if (mask != null && validHit.collider != null)
            {
                painter.PaintMask(mask, validHit, interpolate);
                tool.GetComponent<ToolUtils>().initSound();
            }
        }
    }

    public void enableProcess()
    {
        isStart = true;
    }

    void Update()
    {
        if (isStart)
        {
            Draw();
        }
        else
        {
            painter.desligarParticulas(lascasDeMadeira);
            painter.desligarParticulas(poDeMadeira);
        }
    }

    void marcarEtapa(ref bool etapa)
    {
        if (!etapa)
        {
            etapa = true;
        }
    }

    public bool isPainted()
    {
        return isPaint;
    }

    public Texture getTexture(string chave)
    {
        return textureDictionary[chave];
    }

    public void resetValues()
    {
        isStart = false;

        isSketched = false;
        isSculped = false;
        isSanded = false;
        isPaint = false;
    }

    public bool getSketched()
    {
        return isSketched;
    }

    public void SetSketched(bool state)
    {
        isSketched = state;
    }

    public bool getSculped()
    {
        return isSculped;
    }

    public void SetSculped(bool state)
    {
        isSculped = state;
    }

    public bool getSanded()
    {
        return isSanded;
    }

    public void setSanded(bool state)
    {
        isSanded = state;
    }

    public bool getPaint()
    {
        return isPaint;
    }

    public void setPaint(bool state)
    {
        isPaint = state;
    }
}
