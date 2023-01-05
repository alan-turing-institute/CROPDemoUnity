using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is attached to the CameraHolder GameObject.
public class MovePlayerCamera : MonoBehaviour
{
    public Transform cameraPosition;
    

    // Update is called once per frame
    void Update()
    {
        transform.position = cameraPosition.position;
    }
}
