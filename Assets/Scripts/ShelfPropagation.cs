using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using JsonSchema;


public class ShelfPropagation : MonoBehaviour
{

    Color defaultColour = new Color(0F, 131F, 0F);
    public Dictionary<int, CropData> cropDataForColumn; 

    public Dictionary<string, Color> cropColourDict;
    public Dictionary<string, Color> harvestColourDict;

 

    void Start() {
        //placing random values to crop data on tray objects in scene
        ColourShelvesDefault();
    }

    public void ColourShelvesDefault() {
        for (int i = 0; i < 4; i++) {   
            // find the shelf via the transform child
            Transform shelfTransform = this.transform.GetChild(i);
            // and the corresponding GameObject, so that we can set its name
            GameObject shelf = shelfTransform.gameObject;
            shelf.name = this.name + "-" + (i+1).ToString();
            // set the layer of the shelf
            int shelfLayer = LayerMask.NameToLayer("Shelf");
            shelf.layer = shelfLayer;
            // add a box collider to the shelf
            BoxCollider bc = shelf.AddComponent<BoxCollider>() as BoxCollider;
            // set the colour of the shelf.
            shelfTransform.GetComponent<Renderer>().material.color = defaultColour;
        }
    }

    public void ColourShelvesCropType() {
        if (cropDataForColumn == null ) return;
        foreach(KeyValuePair<int, CropData> entry in cropDataForColumn) {
            // shelves as children of transform will be numbered 0-3 not 1-4
            int shelfIndex = entry.Key - 1;
            CropData cropData = entry.Value;
            Transform shelfTransform = this.transform.GetChild(shelfIndex); 
            shelfTransform.GetComponent<Renderer>().material.color = cropColourDict[cropData.name];
        }   
    }

    // given a date in format YYYY-MM-DD, calculate how many days it is away from today
    int CalculateDayDifference(string dateString) {
        DateTime today = DateTime.Now;
        print("Trying to parse date "+dateString);
        DateTime dt = DateTime.Parse(dateString); //, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var dateDiff = dt - today;
        return dateDiff.Days;
    }

    public void ColourShelvesHarvestTime() {
        if (cropDataForColumn == null ) return;
        foreach(KeyValuePair<int, CropData> entry in cropDataForColumn) {
            // shelves as children of transform will be numbered 0-3 not 1-4
            int shelfIndex = entry.Key - 1;
            CropData cropData = entry.Value;
            Transform shelfTransform = this.transform.GetChild(shelfIndex); 
            string dayDiffStr = cropData.next_action_time;
            if (dayDiffStr != "unknown/none") {
                int dayDiff = CalculateDayDifference(dayDiffStr);
                if (dayDiff > 7) dayDiff = 7;
                if (dayDiff < 1) dayDiff = 1;
                dayDiffStr = dayDiff.ToString();
            }
            shelfTransform.GetComponent<Renderer>().material.color = harvestColourDict[dayDiffStr];
        }   
    }
}
