using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MarcadorController : MonoBehaviour
{
    public TextMeshProUGUI textTutorial;
    [Header("Objects")]
    public GameObject[] ganchos;
    public GameObject marcador;
    public GameObject[] icons;
    public Material outline;

    [Header("Controllers")]
    public XiloController xiloController;
    public GlassController glassController;
    public GrabController grabController;
    public PaperController paperController;

    [Header("Mode")]
    public ExperienceMode mode;

    private MeshRenderer currentTool = null;

    private void Start()
    {
        Reset();
    }

    public void Reset()
    {
        textTutorial.text = "";
        marcador.gameObject.SetActive(false);
        for (int i = 0; i < icons.Length; i++)
            icons[i].SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if ((mode.mode == Mode.VR && grabController.isToolNull()) || (mode.mode == Mode.PROJECTION && !mode.GetComponent<ProjectionMode>().isToolInUse()))
            atualizarMarcador();
    }

    string[] Instructions = {
        "Use o Lapis para revelar o desenho",
        "Use a goiva na vertical para entalhar",
        "Use a lixa",
        "Pegue o pote de tinta e derrame no vidro",
        "Use o rolo de tinta para transferir a tinta do vidro para a madeira",
        "Posicione a folha por cima da madeira",
        "Use o baren para transferir o desenho"
    };

    private void atualizarMarcador()
    {
        if (!xiloController.isStart)
            return;

        string tutorialText = "";
        //int ganchoIndex = 0;

        int index = paperController.isSheetPositioned() ? 5 :
                    xiloController.getPaint() ? 5 :
                    glassController.getInkEnable() ? 4 :
                    xiloController.getSanded() ? 3 :
                    xiloController.getSculped() ? 2 :
                    xiloController.getSketched() ? 1 : 0;

        tutorialText = Instructions[index];

        if (mode.mode == Mode.PROJECTION)
            refreshIcon(index);

        textTutorial.text = tutorialText;
        marcador.gameObject.SetActive(true);
        marcador.transform.position = ganchos[index].transform.position;

        if(currentTool != null && currentTool.sharedMaterials.Length > 1)
        {
            Material[] materials = currentTool.sharedMaterials;
            System.Array.Resize(ref materials, materials.Length - 1);
            currentTool.sharedMaterials = materials;
        }

        //
        MeshRenderer renderer = ganchos[index].transform.GetChild(0).GetComponent<MeshRenderer>();
        currentTool = renderer;
        Material[] currentMaterials = renderer.materials;
        if (currentMaterials.Length > 1)
            return;
        Material[] newMaterials = new Material[currentMaterials.Length + 1];

        for (int i = 0; i < currentMaterials.Length; i++)
            newMaterials[i] = currentMaterials[i];

        newMaterials[newMaterials.Length - 1] = outline;

        renderer.materials = newMaterials;
        //marcador.transform.GetChild(1).GetComponent<MeshRenderer>()materials(outline);
    }

    private void refreshIcon(int index) {
        for (int i = 0; i < icons.Length; i++)
            icons[i].SetActive(i == index);
    }
}
