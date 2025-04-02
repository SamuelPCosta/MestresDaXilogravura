using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class GrabController : MonoBehaviour
{
    public static GrabController instance = null;
    private XRGrabInteractable ferramenta;
    private XRSocketInteractor socket;

    [Header("Controllers")]
    public XiloController xiloController;
    public GlassController glassController;
    public Painter painter;
    
    // Start is called before the first frame update
    void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        reposicionarFerramenta();
    }

    private void reposicionarFerramenta()
    {
        Rigidbody rb;
        if (ferramenta != null)
        {
            rb = ferramenta.GetComponent<Rigidbody>();
            if (rb != null && rb.useGravity)
            {
                if (ferramenta.gameObject.GetComponent<AudioSource>().isPlaying)
                {
                    ferramenta.gameObject.GetComponent<AudioSource>().Stop();
                    //painter.setVerifSound(true);
                }
                socket.allowHover = true;
                ferramenta.transform.position = socket.transform.position;
            }
        }
    }

    public void setTool(XRGrabInteractable ferramenta)
    {
        this.ferramenta = ferramenta;
        ferramenta.transform.Rotate(Vector3.right, -30f);
        //Debug.LogError("saiu");
    }

    public void setSocket(XRSocketInteractor socket)
    {
        this.socket = socket;
    }

    public void resetTool()
    {
        if(ferramenta != null)
            ferramenta.transform.Rotate(Vector3.right, 30f);
        ferramenta = null;
        FindObjectOfType<MenuController>().enableTextIndicator(false);
    }

    public bool isGrab(GameObject tool){
        XRGrabInteractable ferramenta = tool.GetComponent<XRGrabInteractable>();
        if (this.ferramenta == null)
            return false;
        return this.ferramenta.Equals(ferramenta); 
    }

    public bool isToolNull()
    {
        return ferramenta == null;
    }
}
