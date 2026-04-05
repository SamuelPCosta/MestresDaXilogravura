using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit;

public class PaperController : InteractiveObject
{
    private RaycastHit? hit;

    public Painter painter;
    public GrabController grabController;

    public GameObject ferramenta;
    public XiloController xilogravura;

    private bool setarTexturas = false;
    private bool isPrint = false;
    private bool resultado = false;
    private Dictionary<string, RenderTexture> textureDictionary = new Dictionary<string, RenderTexture>();
    private string[] textureNames = { "PrintMask" };

    private bool sheetPositioned = false;
    
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
        int layerMask = 1 << 12;
        if(painter.mode.mode == Mode.PROJECTION && xilogravura.isPainted())
            layerMask = 1 << 10;// CORRIGE O PROBLEMA DA COLHER NA PROJECAO
        if ((hit = painter.CheckDraw(ferramenta, layerMask, true, resultado, null)) != null)
        {
            if (!setarTexturas)
            {
                painter.SetBrushPreset(Brush.Ink);
                currentMaterial.SetTexture("SketchMask", xilogravura.getTexture("SketchMask"));
                currentMaterial.SetTexture("SculptMask", xilogravura.getTexture("SculptMask"));
                currentMaterial.SetTexture("PaintMask", xilogravura.getTexture("PaintMask"));
                setarTexturas = true;
            }
            marcarEtapa(ref isPrint);
        }

        if (hit != null)
        {
            RaycastHit validHit = hit.Value;
            validHit.point = new Vector3(-validHit.point.x, validHit.point.y, validHit.point.z); //TESTE INVTERTER X

            painter.PaintMask(textureDictionary["PrintMask"], validHit, false);
            ferramenta.GetComponent<Tool>().initSound();
            //if (validHit.collider == null || (painter.mode.mode == Mode.VR && grabController.isToolNull()))
            //    painter.stopSound(ferramenta.gameObject);
        }
    }

    void marcarEtapa(ref bool etapa)
    {
        if (!etapa)
        {
            etapa = true;
        }
    }

    public bool isPrinted()
    {
        return isPrint;
    }

    void Update()
    {
        if(sheetPositioned)
            Draw();
    }

    public void posicionarFolha()
    {
        transform.GetChild(1).GetComponent<AudioSource>().Play();
        Animator animator = GetComponent<Animator>();
        animator.SetBool("isPrint", true);
        sheetPositioned = true;
    }
    
    public bool isSheetPositioned()
    {
        return sheetPositioned;
    }

    public void mostrarResultado()
    {
        resultado = true;
        transform.GetChild(2).GetComponent<AudioSource>().Play();
        Animator animator = GetComponent<Animator>();
        animator.SetBool("isResult", true);
    }

    public void resetValues()
    {
        setarTexturas = false;
        isPrint = false;
        resultado = false;
        sheetPositioned = false;
        Animator animator = GetComponent<Animator>();
        animator.SetBool("isPrint", false);
        animator.SetBool("isResult", false);
        animator.Play("EstadoInicial");
        transform.position = new Vector3(2.8f, 3.3f, 1.2f);
    }

}
