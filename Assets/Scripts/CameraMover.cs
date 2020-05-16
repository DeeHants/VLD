using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMover : MonoBehaviour {
    public GameObject forwardRef = null;

    void Update () {
        this.transform.position += this.forwardRef.transform.forward * Input.GetAxis ("Vertical");
        this.transform.position += this.forwardRef.transform.right * Input.GetAxis ("Horizontal");
        this.transform.Rotate (this.forwardRef.transform.right, -Input.GetAxis ("Vertical2") * 5, Space.World);
        this.transform.Rotate (this.transform.up, Input.GetAxis ("Horizontal2") * 5, Space.World);
    }
}