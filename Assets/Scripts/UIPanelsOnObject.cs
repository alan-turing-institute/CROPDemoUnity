using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// An instance of this script is attached to the "textposition" GameObject, which is a child of 
// every sensor GameObject.
public class UIPanelsOnObject : MonoBehaviour
{

    public GameObject label;

    Camera mainCam;
    Camera topDownCam;
    Camera currentCam;

    void Start() {
        GameObject camBase = GameObject.Find("Cameras");
        Transform topDownTransform = camBase.transform.Find("TopDownCam/TopDownCameraHolder/TopDownCamera");
        topDownCam = topDownTransform.gameObject.GetComponent<Camera>();
        Transform mainTransform = camBase.transform.Find("MainCam/Main Camera");
        mainCam = mainTransform.gameObject.GetComponent<Camera>();
        UseMainCam();
    }

    public void UseMainCam() {
        currentCam = mainCam;
    }

    public void UseTopDownCam() {
        currentCam = topDownCam;
    }


    // Update is called once per frame
    void Update()
    {
        if (mainCam.gameObject.active) UseMainCam();
        else UseTopDownCam();
        //get position of object on canvas
        Vector3 canvas_pos = currentCam.WorldToScreenPoint(this.transform.position);
        //correct z position
        Vector3 pos_2d = new Vector3(canvas_pos.x, canvas_pos.y, 0);
        label.transform.position = pos_2d;
    }
}
