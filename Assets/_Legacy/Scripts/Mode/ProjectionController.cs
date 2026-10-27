using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectionController : MonoBehaviour
{
    [Header("Atributos da projeção")]
    [Tooltip("- para o usuário (em metros)")]
    [Range(0f, 5f)][SerializeField] private float distance;

    public void Start()
    {
        transform.position += transform.forward * distance;
    }
}
