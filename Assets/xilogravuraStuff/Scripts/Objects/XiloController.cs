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

    public bool isSketched { get; set; } = false;
    public bool isSculped { get; set; } = false;
    public bool isSanded { get; set; } = false;
    public bool isPaint { get; set; } = false;

    private Dictionary<string, RenderTexture> textureDictionary = new Dictionary<string, RenderTexture>();
    private string[] textureNames = { "SketchMask", "SculptMask", "SandpaperMask", "PaintMask", "PrintMaskOld" };

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
    public void enableProcess()
    {
        isStart = true;
    }

    public bool isPainted()
    {
        return isPaint;
    }

    public Texture getTexture(string key)
    {
        return textureDictionary[key];
    }

    public void resetValues()
    {
        isStart = false;
        isSketched = false;
        isSculped = false;
        isSanded = false;
        isPaint = false;
    }

    public void SetSketched(bool state) => isSketched = state;
    public void SetSculped(bool state) => isSculped = state;
    public void SetSanded(bool state) => isSanded = state;
    public void SetPaint(bool state) => isPaint = state;
}
