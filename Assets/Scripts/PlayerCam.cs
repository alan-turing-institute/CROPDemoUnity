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

        // see if we're looking at a shelf
        bool lookShelf = LookingAtShelf();
        if (lookShelf) {
            noLookReticle.SetActive(false);
            lookReticle.SetActive(true);
            shelfDataHolder.UpdateInfoPanel(hitData.transform.gameObject.name);
        } else {
            noLookReticle.SetActive(true);
            lookReticle.SetActive(false);
        }

        // listen for clicks
        if (Input.GetKey("mouse 0")) {
            if (! mouseButtonDown) {
                shelfDataHolder.ShowInfoPanel(lookShelf);
                mouseButtonDown = true;
            }
        } else {
            if (mouseButtonDown) {
                mouseButtonDown = false;
                shelfDataHolder.HideInfoPanel();
            }
        }

    }

    private bool LookingAtShelf() {
        // use a RayCast from the centre of the ViewPort
        ray = GetComponent<Camera>().ViewportPointToRay(new Vector3 (0.5f, 0.5f, 0));
        int shelfLayer = LayerMask.NameToLayer("Shelf");
        return Physics.Raycast(ray, out hitData, 5000, 1<<shelfLayer);
    }

}