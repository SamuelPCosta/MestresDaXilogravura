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

    //[Header("Tools")]
    //public GameObject lapisDeRascunho;
    //public GameObject goiva;
    //public GameObject lixa;
    //public GameObject roloDeTinta;

    [Header("Particles")]
    public ParticleSystem lascasDeMadeira;
    public ParticleSystem poDeMadeira;

    public bool isStart = false;

    private Dictionary<string, RenderTexture> textureDictionary = new Dictionary<string, RenderTexture>();
    //private string[] textureNames = { "SketchMask", "SculptMask", "SandpaperMask", "PaintMask", "PrintMaskOld" };

    void Start()
    {
        currentMaterial = GetComponent<MeshRenderer>().materials[0];
    }

    //public void resetTextures()
    //{
    //    for (int i = 0; i < textureNames.Length; i++){
    //        Graphics.SetRenderTarget(textureDictionary[textureNames[i]]);
    //        GL.Clear(true, true, Color.black);
    //    }
    //}

    public void ResetOneTexture(string textureName)
    {
        Graphics.SetRenderTarget(textureDictionary[textureName]);
        GL.Clear(true, true, Color.black);
    }

    public void enableProcess()
    {
        isStart = true;
    }

    public Texture getTexture(string key)
    {
        return textureDictionary[key];
    }

    public void resetValues()
    {
        isStart = false;
    }
}
