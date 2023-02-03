using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using JsonSchema;
using System;

//This module is attached to the ReadingsPanel GameObject within the SensorPrefab found in the prefabs folder. 
public class CreateGraphs : MonoBehaviour
{
    public GameObject graphPrefab;
    public List<GameObject> graphs = new List<GameObject>();
    RectTransform rectTransform;

    void Start() {
        rectTransform = gameObject.GetComponent<RectTransform>();
    }


    public void AddGraph(string title, string units, List<float> values) {
        GameObject graphGO = Instantiate(graphPrefab);
        // make it a child of the ReadingsPanel GameObject
        graphGO.transform.SetParent(transform);
        graphGO.GetComponent<RectTransform>().localPosition = new Vector2(0,-90*graphs.Count);
        SensorGraph sgScript = graphGO.GetComponent<SensorGraph>();
        sgScript.SetTitle(title);
        sgScript.SetUnits(units);
        sgScript.SetValue(values[0].ToString());
        float maxVal = values.Max() * 1.4f;
        sgScript.ShowGraph(values, maxVal);
        graphs.Add(graphGO);

        rectTransform.sizeDelta = new Vector2(130, 90*graphs.Count);
    }


}
