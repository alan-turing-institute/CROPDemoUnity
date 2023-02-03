using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// An instance of this script is attached to the "textposition" GameObject, which is a child of 
// every sensor GameObject.
public class UIPanelsOnObject : MonoBehaviour
{

    public GameObject label;
    public GameObject sensorMesh;

    Camera playerCam;
    

    void Start() {
        GameObject camBase = GameObject.Find("Cameras");
        Transform cameraTransform = camBase.transform.Find("FirstPersonCam/PlayerCameraHolder/PlayerCamera");
        playerCam = cameraTransform.gameObject.GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        //get position of object on canvas
        //Vector3 canvasPos = playerCam.WorldToScreenPoint(this.transform.position);
        Vector3 canvasPos = playerCam.WorldToScreenPoint(sensorMesh.transform.position);
        //correct z position
        Vector3 pos2d = new Vector3(canvasPos.x, canvasPos.y, 0);
        label.transform.position = pos2d;
    }
}
