using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectionController : MonoBehaviour
{
    [Header("Atributos da projeção")]
    [Tooltip("- para o usuário (em metros)")]
    [Range(0.1f, 5f)][SerializeField] private float distaciaDaCamera;
    [Tooltip("(em metros)")]
    [Range(0.5f, 1.5f)][SerializeField] private float AlturaDaCamera;

    [Header("Componentes")]
    public Transform virtualCamera;
    //public Transform Tools;

    private bool setVideo = false;

    public void Start()
    {
        float distancia = Mathf.Abs(virtualCamera.localPosition.z) - distaciaDaCamera;
    }
}
