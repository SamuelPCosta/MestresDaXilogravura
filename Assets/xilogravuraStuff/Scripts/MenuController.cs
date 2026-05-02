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
    //public Button createYourArt;
    public GameObject desenho;
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

    private int indice = 0;
    private int indiceAnterior = 0;
    private bool switchImage = false;
    private bool verifStart = false;
    private bool artAutoral = false;
    public Sprite[] desenhos;
    public GameObject matriz;
    public GameObject vidro;
    public GameObject papel;
    public Rect cropRect;


    [Header("Only Projection")]
    public Slider slider;
    public GameObject save;

    public float detectionThreshold = 0.35f;

    public Color colorSelect = Color.green;
    public Color colorDeselect= Color.white;
    public Color colorDisable = Color.gray;
    public TextMeshProUGUI textBrushStatus;
    public GameObject menu;
    public GameObject draw;
    public GameObject firstInstruction;
    public AudioSource audioSource;

    private Sprite spriteTexture;

    // Start is called before the first frame update
    void Start()
    {
        spriteTexture = desenhos[indice];
        drawingCurrent = GameObject.Find("Art");
        
        posicionarFolhaButton = posicionarFolhaMenu.GetComponentInChildren<Button>();
        resultadoButton = resultadoMenu.GetComponentInChildren<Button>();
        restartButton = restartMenu.GetComponentInChildren<Button>();

        undoMenu.SetActive(false);
        posicionarFolhaMenu.SetActive(false);
        resultadoMenu.SetActive(false);
        restartMenu.SetActive(false);

        //firstInstruction?.SetActive(true);

        start.onClick.AddListener(() => StartExp());
        voltar.onClick.AddListener(() => ReturnProcess());
        //createYourArt.onClick.AddListener(() => Invoke("Create", 1f));

        posicionarFolhaButton.onClick.AddListener(() => posicionarFolha());
        resultadoButton.onClick.AddListener(() => StartCoroutine(mostarResultado()));
        restartButton.onClick.AddListener(() => restart());

        Invoke("ClearLog", 1f);
    }

    private void ReturnProcess()
    {
        //if (art.activeSelf)
        //{
        //    art.GetComponent<NewArtController>().ReturnProcess();
        //}

        var xilo = matriz.GetComponent<XiloController>();
    }


    // Update is called once per frame
    void Update()
    {
        //Cursor.visible = false;

        //if (matriz.GetComponent<XiloController>().isPainted() && !folhaPosicionada)
        //{
        //    posicionarFolhaMenu.SetActive(true);
        //}
        if (papel.GetComponent<PaperController>().isPrinted() && !folhaResultado)
        {
            voltar.gameObject.SetActive(false);
            resultadoMenu.SetActive(true);
        }

        if ((indice != indiceAnterior))
        {
            spriteTexture = desenhos[indice];
            drawingCurrent.GetComponent<Image>().sprite = spriteTexture;

            indiceAnterior = indice;
            left.interactable = true;
            right.interactable = true;
        }

        //if (art.GetComponent<NewArtController>().isArt())
        //{
        //    start.gameObject.SetActive(true);
        //}
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
        right.interactable = false;
        switchImage = true;
        indice = (indice + 1) % desenhos.Length;
    }

    public void PreviousMenu()
    {
        left.interactable = false;
        switchImage = true;
        indice = (indice - 1 + desenhos.Length) % desenhos.Length;
    }

    private void StartExp()
    {
        //verifStart = true;
        undoMenu.SetActive(true);
        if (menu != null)
        {
            configurarSimulacao();
            menu.SetActive(false);
            draw?.SetActive(false);
            //art?.SetActive(false);
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
        //createYourArt.gameObject.SetActive(false);
        voltar.gameObject.SetActive(true);
        //art?.SetActive(true);
        artAutoral = true;
    }

    void configurarSimulacao()
    {

        //if (artAutoral)
        //{
        //    NewArtController newArtController = art.GetComponent<NewArtController>();
        //    setDesenhoEscolhido(newArtController.getTexture("SketchMask"));
        //}
        //else
        //{
            Sprite spriteAtual = desenhos[indice];
            if (spriteAtual != null){
                setChosenArt(spriteAtual.texture);
            }
        //}
    }

    public void setChosenArt(Texture art)
    {
        matriz.GetComponent<XiloController>().enableProcess();
        Material xiloMaterial = matriz.GetComponent<MeshRenderer>().materials[0];
        xiloMaterial.SetTexture("SketchTexture", art);


        Material paperMaterial = papel.GetComponent<MeshRenderer>().materials[0];
        paperMaterial.SetTexture("SketchTexture", art);

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
        //xiloController.resetTextures();
        xiloController.resetValues();

        GlassController glassController = vidro.GetComponent<GlassController>();
        glassController.resetTextures();
        glassController.resetValues();

        PaperController paperController = papel.GetComponent<PaperController>();
        paperController.resetTextures();
        paperController.resetValues();

        //NewArtController newArtController = art.GetComponent<NewArtController>();
        //newArtController.resetTextures();
        //newArtController.resetValues();

        FindFirstObjectByType<MarcadorController>().Reset();

        //Corrige preset ao reiniciar
        FindFirstObjectByType<Painter>().SetBrushPreset(Brush.HardCircle);

        folhaPosicionada = false;
        folhaResultado = false;

        switchImage = false;
        artAutoral = false;

        undoMenu.SetActive(false);
        //firstInstruction?.SetActive(true);
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
        //if (voltar != null && voltar.IsActive() && art.activeSelf)
        //    ReturnProcess();
        //if (createYourArt != null && createYourArt.IsActive())
        //    Create();
        //else 
        if (voltar != null && voltar.IsActive())
            ReturnProcess();
        else if (restartButton != null && restartButton.IsActive()){
            restart();
            menu.SetActive(true);
            desenho.SetActive(true);
            left.gameObject.SetActive(true);
            right.gameObject.SetActive(true);
            //createYourArt.gameObject.SetActive(true);
        }
        playClick();
    }

    private void playClick()
    {
        audioSource.Play();
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