using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
//using static TreeEditor.TextureAtlas;

public class NewArtController : InteractiveObject
{
    private RaycastHit? hit;

    public Painter painter;
    public GrabController grabController;
    public GameObject matriz;

    public Button voltar;

    public GameObject lapisDeRascunho;

    private bool isSketched = false;

    //[SerializeField] private TouchController touch;

    private Dictionary<string, RenderTexture> textureDictionary = new Dictionary<string, RenderTexture>();
    private string[] textureNames = { "SketchMask" };

    void Start()
    {
        currentMaterial = GetComponent<MeshRenderer>().materials[0];
        voltar.onClick.AddListener(() => ReturnProcess());
        setTextures();
        gameObject.SetActive(false);
    }

    public void ReturnProcess()
    {
        //if(!matriz.GetComponent<XiloController>().isSketched)
        //{
        //    Graphics.SetRenderTarget(textureDictionary["SketchMask"]);
        //    GL.Clear(true, true, Color.black);
        //}
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

    // Update is called once per frame
    void Update()
    {
        Draw();
    }

    // Metodo para debug no simulador
    private bool click()
    {
        return Mouse.current.leftButton.isPressed;
    }

    public void Draw()
    {
        int layerMask = 1 << LayerMask.NameToLayer("newArt");
        if ((hit = painter.CheckDraw(lapisDeRascunho, layerMask, true, false, null)) != null)
        {
            painter.SetBrushPreset(Brush.HardCircle);
            isSketched = true;
        }

        if (hit != null)
        {
            RaycastHit validHit = hit.Value;
            painter.PaintMask(textureDictionary["SketchMask"], validHit, true);
            //if (validHit.collider == null || (painter.mode.mode == Mode.VR && grabController.isToolNull()))
            //    painter.stopSound(lapisDeRascunho.gameObject);
        }
    }

    public void resetValues()
    {
        isSketched = false;
    }

    public bool isArt()
    {
        return isSketched;
    }

    public Texture getTexture(string chave)
    {
        return textureDictionary[chave];
    }
}
