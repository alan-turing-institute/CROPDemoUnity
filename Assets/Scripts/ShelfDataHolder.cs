using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using JsonSchema;

//  Script to be attached to the "Farm" GameObject.   Provide a ShelfData (as defined in JsonSchema)
//  object to a given shelf upon request

public class ShelfDataHolder : MonoBehaviour
{
    public GameObject infoPanel;
    public GameObject shelfText;

    Color defaultColour = new Color(0F, 131F, 0F);
    public Dictionary<int, CropData> cropDataForColumn; 

    public Dictionary<string, Color> cropColourDict;
    public Dictionary<string, Color> harvestColourDict;

    List<string> tunnels = new List<string>{"Tunnel3","Tunnel4", "Tunnel5", "Tunnel6"};

    void Start() {
        ResetShelfText();
    }

    public void ResetShelfText() {
        shelfText.GetComponent<Text>().text = "No information to display";
    }

    public void UpdateInfoPanel(string shelfID) {
        print("Updating the info panel with "+shelfID);
        return;
    }

    public void ToggleInfoPanel() {
        print("Toggling the info panel");
        if (! infoPanel.active) {
            infoPanel.SetActive(true);
        } else {
            infoPanel.SetActive(false);
            ResetShelfText();
        }
    }

    public void ShowInfoPanel() {
        if (! infoPanel.active) {
            infoPanel.SetActive(true);
        }
    }

    public void HideInfoPanel() {   
        if ( infoPanel.active) {
            infoPanel.SetActive(false);
            ResetShelfText();
        }
    }
}
