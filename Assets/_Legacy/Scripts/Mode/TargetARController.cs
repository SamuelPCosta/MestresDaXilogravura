using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetARController : MonoBehaviour
{
    private Vector3 lastPosition = Vector3.zero;
    private Quaternion lastRotation = Quaternion.identity;

    [Header("Settings")]
    //public Transform target;
    [Range(0, 10)] public int stabilization = 5;
    public float limit = 0f;

    // Update is called once per frame
    void Update()
    {
        if(!gameObject.activeSelf)
            return;

        if (Vector3.Distance(transform.position, lastPosition) < stabilization/100)
            return;

        lastPosition = transform.position;
        transform.localPosition = new Vector3(transform.localPosition.x * (-1), transform.localPosition.y, transform.localPosition.z * (-1));
        transform.eulerAngles = new Vector3(transform.eulerAngles.x * (-1), transform.eulerAngles.y, transform.eulerAngles.z * (-1));

        if (transform.localPosition.z > limit)
            transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, limit);
    }
}
