using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Linq;

public enum brushType { ROUND1, SQUARE, ROUND2 };
public enum Steps { SKETCHING, CARVING, SANDING, INKING, PRINTING }; //NEW_ART

public class DynamicPainting : MonoBehaviour
{
    [Header("Cameras and objects")]
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private RenderTexture[] renderTexture;
    [SerializeField] private Transform pointer;
    [SerializeField] private UDPReceiver _UDPReceiver;
    [SerializeField] private Tool tool;

    [Header("Brush")]
    [SerializeField] private Sprite[] spriteBrush;
    [SerializeField] private Color brushColor;
    [Range(1f, 10f)]
    [SerializeField] private int brushSize = 1;
    [SerializeField] private Slider brushSizeSlider;
    [SerializeField] private TextMeshProUGUI brushSizeText;

    [Header("Canvas")]
    [SerializeField] private GameObject[] canvasObjs;
    [SerializeField] private Steps currentStep;

    private GameObject pencil;
    private GameObject gouge;
    private GameObject sandpaper;
    private GameObject ink;
    private GameObject roller;
    private GameObject baren;

    float brushSizeFixed = 1f;
    Material drawSpriteMat;
    private Vector2? lastUV = null;

    //INDICE
    private int maskIndex = 1;

    private void Start()
    {
        drawSpriteMat = new Material(Shader.Find("Hidden/DrawSprite")); //shader responsavel por desenhar na textura
        ClearAllRenderTextures();
        brushSizeSlider.onValueChanged
                       .AddListener(
                            value => setBrushSize(Mathf.RoundToInt(value))
                        );

        pencil = tool.getTool(AvailableTools.PENCIL);
        gouge = tool.getTool(AvailableTools.GOUGE);
        sandpaper = tool.getTool(AvailableTools.SANDPAPER);
        ink = tool.getTool(AvailableTools.INK);
        roller = tool.getTool(AvailableTools.PAINT_ROLLER);
        baren = tool.getTool(AvailableTools.BAREN);
    }

    void Update()
    {
        if (_UDPReceiver.ledStatus)
            CheckPaint();
        else { 
            lastSpriteUV = null;
            tool.stopSound();
        }
    }

    private Vector2? lastSpriteUV = null;
    private void CheckPaint()
    {
        setBrushSize(brushSize);

        Vector3 uvWorldPosition = Vector3.zero;
        if (HitUVPosition(ref uvWorldPosition)){
            if (meshCollider == null)
                return;
            bool allowedLayer = checkStep(meshCollider.gameObject);
            if (!allowedLayer)
                return;

            Vector2 currentUV = new Vector2(uvWorldPosition.x, uvWorldPosition.y);
            DrawSprite(renderTexture[maskIndex], spriteBrush[(int)brushType.ROUND1], currentUV, new Vector2(brushSizeFixed, brushSizeFixed));

            if (lastSpriteUV.HasValue)
                DrawInterpolationLine(renderTexture[maskIndex], spriteBrush[(int)brushType.SQUARE], lastSpriteUV.Value, currentUV, brushSizeFixed);
            else { 
                tool.initSound();
            }

            lastSpriteUV = currentUV;
        }
        else{
            lastSpriteUV = null;
            tool.stopSound();
        }
    }

    private void setBrushSize(int value)
    {
        brushSize = value;
        brushSizeText.text = "" + value;
        brushSizeFixed = remap(value, 2f, 10f, 0.005f, 0.2f);
    }

    void DrawSprite(RenderTexture renderTexure, Sprite sprite, Vector2 uvPos, Vector2 uvScale)
    {
        drawSpriteMat.SetTexture("_SpriteTex", sprite.texture);
        drawSpriteMat.SetVector("_Pos", uvPos);
        drawSpriteMat.SetVector("_Scale", uvScale);

        RenderTexture temp = RenderTexture.GetTemporary(renderTexure.width, renderTexure.height, 0, renderTexure.format);
        Graphics.Blit(renderTexure, temp);
        Graphics.Blit(temp, renderTexure, drawSpriteMat);
        RenderTexture.ReleaseTemporary(temp);
    }

    private void DrawInterpolationLine(RenderTexture renderTexture, Sprite sprite, Vector2 startUV, Vector2 endUV, float thickness)
    {
        Vector2 direction = endUV - startUV;
        float length = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x);

        drawSpriteMat.SetTexture("_SpriteTex", sprite.texture);
        drawSpriteMat.SetVector("_Pos", (startUV + endUV) * 0.5f);
        drawSpriteMat.SetVector("_Scale", new Vector2(length, thickness));
        drawSpriteMat.SetFloat("_Rotation", angle);

        RenderTexture temp = RenderTexture.GetTemporary(renderTexture.width, renderTexture.height, 0, renderTexture.format);
        Graphics.Blit(renderTexture, temp);
        Graphics.Blit(temp, renderTexture, drawSpriteMat);
        RenderTexture.ReleaseTemporary(temp);
    }

    private MeshCollider meshCollider = null;
    private bool HitUVPosition(ref Vector3 uvWorldPosition){
        if (pointer == null) return false;

        Vector3 screenPos = sceneCamera.WorldToScreenPoint(pointer.position);
        if (screenPos.z <= 0) return false;

        Ray ray = sceneCamera.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
        Debug.DrawRay(ray.origin, ray.direction * 10f, Color.green);
        RaycastHit[] hits = Physics.RaycastAll(ray, 10f);

        foreach (var hit in hits){
            meshCollider = hit.collider as MeshCollider;
            if (meshCollider != null && meshCollider.sharedMesh != null && canvasObjs.Contains(meshCollider.gameObject)){
                Vector2 uv = hit.textureCoord;
                uvWorldPosition = new Vector3(uv.x, uv.y, 0f);
                return true;
            }
        }
        return false;
    }

    private bool checkStep(GameObject hitObject)
    {
        GameObject currentTool = tool.getCurrentTool();
        if(currentTool == null) return false;

        bool allowed = false;
        Steps previousStep = currentStep;

        print(LayerMask.LayerToName(hitObject.layer));
        switch (LayerMask.LayerToName(hitObject.layer)){
            case "newArt":
                break;
            case "wood":
                if (allowed = IsValidStep(pencil, currentTool, Steps.SKETCHING))       currentStep = Steps.SKETCHING;
                else if (allowed = IsValidStep(gouge, currentTool, Steps.CARVING))     currentStep = Steps.CARVING;
                else if (allowed = IsValidStep(sandpaper, currentTool, Steps.SANDING)) currentStep = Steps.SANDING;
                break;
            case "glass":
                if (allowed = IsValidStep(ink, currentTool, Steps.INKING)) currentStep = Steps.INKING;
                break;
            case "paper":
                break;
        }

        if (previousStep != currentStep)
            incrementMaskIndex();

        return allowed;
    }

    private bool IsValidStep(GameObject tool, GameObject currentTool, Steps step)
    {
        return (tool == currentTool && (currentStep == step - 1 || currentStep == step));
    }

    float remap(float value, float minIn, float maxIn, float minOut, float maxOut)
    {
        return (value - minIn) / (maxIn - minIn) * (maxOut - minOut) + minOut;
    }

    #region public_methods
    public void setPointer(Transform pointer) //define apontador na ponta da ferramenta atual
    {
        this.pointer = pointer;
    } 
    public void resetPointer() //usado quando se perder o tracking da ferramenta
    {
        pointer = null;
        lastUV = null;
    } 
    public void incrementMaskIndex() //avanca uma etapa
    {
        maskIndex++;
    } 
    public void resetMask() //limpa a mascara atual
    {
        RenderTexture.active = renderTexture[maskIndex];
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = null;
    } 
    public void resetMaskIndex() //usado ao resetar o loop da experiencia
    {
        maskIndex = 0;
    }
    public void ClearAllRenderTextures()
    {
        foreach (var rt in renderTexture)
        {
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.clear);
        }
        RenderTexture.active = null;
    }
    #endregion
}
