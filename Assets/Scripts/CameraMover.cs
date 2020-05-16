using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMover : MonoBehaviour
{
    public GameObject forwardRef = null;

    void Update()
    {
        this.transform.position +=  this.forwardRef.transform.forward * Input.GetAxis("Vertical");
        this.transform.position +=  this.forwardRef.transform.right * Input.GetAxis("Horizontal");
        this.transform.Rotate(new Vector3(0,Input.GetAxis("Horizontal2") * 5, 0),Space.Self);
    }
}
