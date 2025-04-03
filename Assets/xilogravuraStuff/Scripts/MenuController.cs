using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class MenuController : MonoBehaviour
{
    public Button start;
    public Button createYourArt;
    public GameObject desenho;
    public GameObject tituloMenu;
    public Button left;
    public Button right;
    public Button voltar;

    [SerializeField]
    private TextMeshProUGUI textTutorial;

    private GameObject canva;
    public GameObject undoMenu;
    public GameObject posicionarFolhaMenu;
    public GameObject resultadoMenu;
    public GameObject restartMenu;

    public GameObject outOfRangText;

    private Button posicionarFolhaButton;
    private Button resultadoButton;
    private Button restartButton;

    private bool folhaPosicionada = false;
    private bool folhaResultado = false;

    private GameObject drawingCurrent;

    private int indice=0;
    private int indiceAnterior = 0;
    private bool switchImage = false;
    private bool verifStart = false;
    private bool artAutoral = false;
    public Sprite[] desenhos;
    public GameObject matriz;
    public GameObject vidro;
    public GameObject papel;
    public GameObject art;
    public Rect cropRect;


    [Header("Only Projection")]
    public Slider slider;
    public GameObject save;

    private bool unlock = true;
    private Transform tool = null;

    //public float detectionInterval = 0.5f;
    public float detectionThreshold = 0.35f;
    private float lastDetectionTime;
    private float lastPosX;

    public Image bigArrowLeft;
    public Image bigArrowRight;
    public Color colorSelect = Color.green;
    public Color colorDeselect= Color.white;
    public Color colorDisable = Color.gray;
    public TextMeshProUGUI textBrushStatus;
    public GameObject menu;
    public GameObject firstInstruction;
    public AudioSource audioSource;

    // Start is called before the first frame update
    void Start()
    {
        drawingCurrent = GameObject.Find("Desenho");
        
        posicionarFolhaButton = posicionarFolhaMenu.GetComponentInChildren<Button>();
        resultadoButton = resultadoMenu.GetComponentInChildren<Button>();
        restartButton = restartMenu.GetComponentInChildren<Button>();

        undoMenu.SetActive(false);
        posicionarFolhaMenu.SetActive(false);
        resultadoMenu.SetActive(false);
        restartMenu.SetActive(false);

        //firstInstruction?.SetActive(true);

        enableTextIndicator(false);

        //left.onClick.AddListener(() => PreviousMenu());
        //right.onClick.AddListener(() => NextMenu());
        start.onClick.AddListener(() => StartExp());
        voltar.onClick.AddListener(() => ReturnProcess());
        createYourArt.onClick.AddListener(() => Invoke("Create", 1f));

        posicionarFolhaButton.onClick.AddListener(() => posicionarFolha());
        resultadoButton.onClick.AddListener(() => StartCoroutine(mostarResultado()));
        restartButton.onClick.AddListener(() => restart());

        Invoke("ClearLog", 1f);
    }

    private void ReturnProcess()
    {
        if (art.activeSelf)
        {
            art.GetComponent<NewArtController>().ReturnProcess();
        }

        if (matriz.GetComponent<XiloController>().getPaint())
        {
            matriz.GetComponent<XiloController>().ResetOneTexture("PaintMask");
            matriz.GetComponent<XiloController>().setPaint(false);
        }
        else if (matriz.GetComponent<XiloController>().getSanded())
        {
            matriz.GetComponent<XiloController>().ResetOneTexture("SandMask");
            matriz.GetComponent<XiloController>().setSanded(false);
        }
        else if (matriz.GetComponent<XiloController>().getSculped())
        {
            matriz.GetComponent<XiloController>().ResetOneTexture("SculptMask");
            matriz.GetComponent<XiloController>().SetSculped(false);
        }
        else if (matriz.GetComponent<XiloController>().getSketched())
        {
            matriz.GetComponent<XiloController>().ResetOneTexture("SketchMask");
            matriz.GetComponent<XiloController>().SetSketched(false);
        }  
    }

    // Update is called once per frame
    void Update()
    {
        //Cursor.visible = false;

        if (matriz.GetComponent<XiloController>().isPainted() && !folhaPosicionada)
        {
            posicionarFolhaMenu.SetActive(true);
        }
        if (papel.GetComponent<PaperController>().isPrinted() && !folhaResultado)
        {
            voltar.gameObject.SetActive(false);
            resultadoMenu.SetActive(true);
        }

        if (!verifStart && (indice != indiceAnterior || !switchImage))
        {
            Sprite spriteTexture = Sprite.Create(desenhos[indice].texture, cropRect, new Vector2(0.5f, 0.5f));
            drawingCurrent.GetComponent<Image>().sprite = spriteTexture;

            indiceAnterior = indice;
            left.enabled = true;
            right.enabled = true;
        }

        if (art.GetComponent<NewArtController>().isArt())
        {
            start.gameObject.SetActive(true);
        }

        if (tool != null && tool.gameObject.activeSelf)
            checkArrowsProjection();
    }

    void posicionarFolha()
    {
        textTutorial.text = "Pegue a colher";
        PaperController paperController = papel.GetComponent<PaperController>();
        paperController.posicionarFolha();
        folhaPosicionada = true;
        posicionarFolhaMenu.SetActive(false);
    }

    IEnumerator mostarResultado()
    {
        PaperController paperController = papel.GetComponent<PaperController>();
        paperController.mostrarResultado();
        folhaResultado = true;
        resultadoMenu.SetActive(false);

        yield return new WaitForSeconds(3f);

        restartMenu.SetActive(true);
    }

    public void NextMenu()
    {
        right.enabled = false;
        switchImage = true;
        if (indice == desenhos.Length-1)
        {
            indice = 0;
        }
        else
        {
            indice++;
        }
    }

    public void PreviousMenu()
    {
        left.enabled = false;
        switchImage = true;
        if(indice <= 0)
        {
            indice = desenhos.Length-1;
        }
        else
        {
            indice--;
        }
    }

    private void StartExp()
    {
        //verifStart = true;
        undoMenu.SetActive(true);
        canva = GameObject.Find("Menu");
        if (canva != null)
        {
            configurarSimulacao();
            canva.SetActive(false);
            art?.SetActive(false);
            voltar.gameObject.SetActive(true);
        }
        //firstInstruction?.SetActive(false);
    }

    private void Create()
    {
        left.gameObject.SetActive(false);
        right.gameObject.SetActive(false);
        start.gameObject.SetActive(false);
        desenho.SetActive(false);
        tituloMenu.SetActive(false);
        createYourArt.gameObject.SetActive(false);
        voltar.gameObject.SetActive(true);
        art?.SetActive(true);
        artAutoral = true;
    }

    void configurarSimulacao()
    {

        if (artAutoral)
        {
            NewArtController newArtController = art.GetComponent<NewArtController>();
            setDesenhoEscolhido(newArtController.getTexture("SketchMask"));
        }
        else
        {
            Sprite spriteAtual = desenhos[indice];
            if (spriteAtual != null)
            {
                Texture2D desenho = new Texture2D((int)spriteAtual.rect.width, (int)spriteAtual.rect.height);

                desenho.SetPixels(spriteAtual.texture.GetPixels((int)spriteAtual.rect.x, (int)spriteAtual.rect.y,
                    (int)spriteAtual.rect.width, (int)spriteAtual.rect.height));

                desenho.Apply();

                setDesenhoEscolhido(desenho);
            }
        }
    }

    public void setDesenhoEscolhido(Texture desenho)
    {
        matriz.GetComponent<XiloController>().enableProcess();
        matriz.GetComponent<XiloController>().setTextures();
        Material xiloMaterial = matriz.GetComponent<MeshRenderer>().materials[0];
        xiloMaterial.SetTexture("SketchTexture", desenho);

        Material paperMaterial = papel.GetComponent<MeshRenderer>().materials[0];
        paperMaterial.SetTexture("SketchTexture", desenho);

        if (artAutoral)
        {
            xiloMaterial.SetFloat("autoral", 1);
            paperMaterial.SetFloat("autoral", 1);
        }
        else
        {
            xiloMaterial.SetFloat("autoral", 0);
            paperMaterial.SetFloat("autoral", 0);
        }
    }

    public void restart()
    {
        //E eu quis escrever um codigo que pudesse te fazer sentir [...]
        //com uma bela identacao pra dizer o que eu nao consigo documentar
        restartMenu.SetActive(false);
        XiloController xiloController = matriz.GetComponent<XiloController>();
        xiloController.resetTextures();
        xiloController.resetValues();

        GlassController glassController = vidro.GetComponent<GlassController>();
        glassController.resetTextures();
        glassController.resetValues();

        PaperController paperController = papel.GetComponent<PaperController>();
        paperController.resetTextures();
        paperController.resetValues();

        NewArtController newArtController = art.GetComponent<NewArtController>();
        newArtController.resetTextures();
        newArtController.resetValues();

        FindObjectOfType<MarcadorController>().Reset();

        //Corrige preset ao reiniciar
        FindObjectOfType<Painter>().SetBrushPreset(Brush.HardCircle);

        enableTextIndicator(false);

        folhaPosicionada = false;
        folhaResultado = false;

        switchImage = false;
        artAutoral = false;

        undoMenu.SetActive(false);
        //firstInstruction?.SetActive(true);
    }

    public void enableTextIndicator(bool state)
    {
        outOfRangText?.SetActive(state);
    }

    public void firstOptionByProjection()
    {
        if (start != null && start.IsActive())
            StartExp();
        else if (posicionarFolhaButton != null && posicionarFolhaButton.IsActive())
            posicionarFolha();
        else if (resultadoButton != null && resultadoButton.IsActive())
            StartCoroutine(mostarResultado());
        else if (save != null && save.activeSelf)
            papel.GetComponent<ShaderBaker>().captureTexture();
        playClick();
    }

    public void secondOptionByProjection()
    {
        if (voltar != null && voltar.IsActive() && art.activeSelf)
            ReturnProcess();
        if (createYourArt != null && createYourArt.IsActive())
            Create();
        else if (voltar != null && voltar.IsActive())
            ReturnProcess();
        else if (restartButton != null && restartButton.IsActive()){
            restart();
            menu.SetActive(true);
            desenho.SetActive(true);
            left.gameObject.SetActive(true);
            right.gameObject.SetActive(true);
            createYourArt.gameObject.SetActive(true);
            tituloMenu.SetActive(true);
        }
        playClick();
    }

    private void playClick()
    {
        audioSource.Play();
    }

    public void setArrow(Transform tool)
    {
        this.tool = tool;
        tool.transform.localPosition = new Vector3(0f, 0f, 0f);
    }

    public void resetArrow()
    {
        tool = null;
    }

    private void checkArrowsProjection()
    {
        if (unlock){ 
            if (tool.transform.localPosition.x <= -detectionThreshold){
                if (right.IsActive())
                {
                    NextMenu();
                    unlock = false;
                    bigArrowRight.color = colorSelect;
                }
                else{
                    //if(slider.value != slider.maxValue){
                    //    slider.value = Mathf.Clamp(slider.value + 1, slider.minValue, slider.maxValue);
                    //    unlock = false;
                    //    bigArrowRight.color = colorSelect;
                    //}
                }
            }
            else if (tool.transform.localPosition.x >= detectionThreshold){
                if (left.IsActive()){
                    PreviousMenu();
                    unlock = false;
                    bigArrowLeft.color = colorSelect;
                }
                else
                {
                    //if (slider.value != slider.minValue)
                    //{
                    //    slider.value = Mathf.Clamp(slider.value - 1, slider.minValue, slider.maxValue);
                    //    unlock = false;
                    //    bigArrowLeft.color = colorSelect;
                    //}
                }
            }
        }
        if (tool.transform.localPosition.x > -detectionThreshold && tool.transform.localPosition.x < detectionThreshold)
        {
            unlock = true;
            if (slider.value == slider.maxValue)
                bigArrowRight.color = colorDisable;
            else
                bigArrowRight.color = colorDeselect;

            if (slider.value == slider.minValue)
                bigArrowLeft.color = colorDisable;
            else
                bigArrowLeft.color = colorDeselect;
        }
        //if (!right.IsActive() && !left.IsActive()){
        //    textBrushStatus.gameObject.SetActive(true);
        //    textBrushStatus.text = "Tamanho do pincel: " + displaySlidValue();
        //}
        //else
        //    textBrushStatus.gameObject.SetActive(false);
    }

    private float displaySlidValue()
    {
        if (slider != null)
            return slider.value;
        else return 0;
    }
    
    #if UNITY_EDITOR
    public void ClearLog()
    {
        var assembly = System.Reflection.Assembly.GetAssembly(typeof(SceneView));
        var logEntries = assembly.GetType("UnityEditor.LogEntries");
        var clearMethod = logEntries.GetMethod("Clear");
        clearMethod.Invoke(null, null);
        Debug.Log("Lets go!");
    }
    #endif
}