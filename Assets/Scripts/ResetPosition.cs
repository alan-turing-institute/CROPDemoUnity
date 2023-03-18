using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class ResetPosition : MonoBehaviour
{
    public GameObject leftControllerGO;
    XRController leftController;
    UnityEngine.XR.InputDevice device;

    Vector3 initialPosition;
    Quaternion initialRotation;

    bool triggerPressedPreviousFrame = false;
    int triggerPressCounter = 0;

    // Start is called before the first frame update
    void Start()
    {
        leftController = leftControllerGO.GetComponent<XRController>();
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        var leftHandDevices = new List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevicesAtXRNode(UnityEngine.XR.XRNode.LeftHand, leftHandDevices);

        if(leftHandDevices.Count == 1)
        {
            device = leftHandDevices[0];
            Debug.Log(string.Format("Device name '{0}' with role '{1}'", device.name, device.role.ToString()));
        }
    }

    // Update is called once per frame
    public void Reset()
    {
        GameObject ip = GameObject.Find("InfoPanelLobby");
        GameObject sphere = ip.transform.Find("TestSphere").gameObject;
        if (sphere.active) sphere.SetActive(false); 
        else sphere.SetActive(true);
        transform.position = initialPosition;
        transform.rotation = initialRotation;
    }

    private void Update() {
        if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerButtonPressed) && triggerButtonPressed)
        {
            if (! triggerPressedPreviousFrame) {
                triggerPressCounter += 1;
                triggerPressedPreviousFrame = true;
            }
        } else {
            triggerPressedPreviousFrame = false;
        }
        if (triggerPressCounter == 5) {
            Reset();
            triggerPressCounter = 0;
        }
    }
}
