using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This script is attached to the "PlayerCamera" GameObject.
public class PlayerCam : MonoBehaviour {
    public float sensX;
    public float sensY;

    public Transform orientation;
    public GameObject lookReticle;
    public GameObject noLookReticle;
    public GameObject farm;
    ShelfDataHolder shelfDataHolder;

    float xRotation;
    float yRotation;

    Ray ray;
    RaycastHit hitData;

    bool mouseButtonDown = false;
    SensorMethods sensorScript;
    enum InfoType {
        Shelf,
        Sensor
    }

    InfoType infoToShow;

    private void Start() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        shelfDataHolder = farm.GetComponent<ShelfDataHolder>();
        // set starting direction to look in
        yRotation = -90f;
    }

    private void Update() {
        float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * sensX;
        float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * sensY;

        yRotation += mouseX;
        xRotation -= mouseY;

        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.rotation = Quaternion.Euler(xRotation,yRotation,0);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);

        // see if we're looking at a sensor or shelf
        Transform lookingAt = LookingAtSomething();
        if (lookingAt == null) {
            noLookReticle.SetActive(true);
            lookReticle.SetActive(false);
            if (sensorScript != null) sensorScript.beingLookedAt = false;
        } else {
            noLookReticle.SetActive(false);
            lookReticle.SetActive(true);
            if (lookingAt.gameObject.name == "SensorMesh") {
                infoToShow = InfoType.Sensor;
                sensorScript = lookingAt.parent.gameObject.GetComponent<SensorMethods>();
                sensorScript.beingLookedAt = true;

            } else {
                shelfDataHolder.UpdateInfoPanel(lookingAt.gameObject.name);
                infoToShow = InfoType.Shelf;
            }
        } 

        // listen for mouse down
        if (Input.GetKey("mouse 0")) {
            if (! mouseButtonDown) {
                mouseButtonDown = true;
                if (infoToShow == InfoType.Shelf) {
                    shelfDataHolder.ShowInfoPanel(lookingAt != null);
                } else {
                  sensorScript.DisplayGraphs();
                }
            }
        } else {
            if (mouseButtonDown) {
                mouseButtonDown = false;
                if (infoToShow == InfoType.Shelf) {
                    shelfDataHolder.HideInfoPanel();
                } else {
                    sensorScript.HideGraphs();
                }
            }
        }

    }

    private Transform LookingAtSomething() {
        // use a RayCast from the centre of the ViewPort
        ray = GetComponent<Camera>().ViewportPointToRay(new Vector3 (0.5f, 0.5f, 0));
        int sensorLayer = LayerMask.NameToLayer("Sensor");
        int shelfLayer = LayerMask.NameToLayer("Shelf");
        // look for hit on sensor first
        bool hitSensor = Physics.Raycast(ray, out hitData, 5000, 1<<sensorLayer);
        if (hitSensor) return hitData.transform;
        // if not, look for hit on shelf
        bool hitShelf = Physics.Raycast(ray, out hitData, 5000, 1<<shelfLayer);
        if (hitShelf) return hitData.transform;
        return null;
    }

}