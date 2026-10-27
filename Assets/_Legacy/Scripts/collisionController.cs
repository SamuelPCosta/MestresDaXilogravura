using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class collisionController : MonoBehaviour
{
    public GameObject origin;
    Rigidbody rb;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    /*void FixedUpdate()
    {
        Vector3 newPosition = cam.position;
        transform.position = newPosition + new Vector3(0f, 0f, 0f);
    }*/

    //private void OnCollisionEnter(Collision collision)
    //{
    //    if(collision.transform.CompareTag("parede"))
    //    {
    //        //Debug.Log("paredeee");
    //        Vector3 newPosition = new Vector3(origin.transform.position.x - 0.08f, origin.transform.position.y, origin.transform.position.z);
    //        origin.transform.position = newPosition;
    //    }
    //}
}
