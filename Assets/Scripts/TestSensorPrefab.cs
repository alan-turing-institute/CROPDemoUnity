using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using JsonSchema;
using System;

public class TestSensorPrefab : MonoBehaviour
{
    /* This module is attached to the sensor prefab found in the prefabs folder. 
    the moment of the instantiation, it sends a request to the flask CROP app with the sensor id 
    and gets the current readings.
    example of request: 
    "queries/getadvanticsysdata/1?range=20190201-20190201";

        dependencies:
        "sensors" empty Game object in main scene 

        global variables: 
        connection_string: query to get a list of sensors, along with sensor type.
        sensor_gobj: the 3D model prefab which represents the sensor to be instantiated.
        healthbar:
        manabar:
        graphs:
    */

    void Start() {
        //Sets up the visualisations of the sensors
        SetupUI();

/*
        // get the script that holds the readings
        GameObject sensorsGameObj = GameObject.Find("Sensors");
        if (sensorsGameObj == null) {
            print("Couldn't find 'Sensors' GameObject");
            return;
        }
        SensorReadings sensorReadingsScript = sensorsGameObj.GetComponent<SensorReadings>();
        sensorReadings = sensorReadingsScript.GetAllReadingsForSensorId(sensor.sensor_id, sensor.sensor_type);
        //print("In SensorMethods start - length of readings for "+sensor.sensor_id+" is "+sensorReadings.readingList.Count);
        DisplayCurrentReadings(sensorReadings);
        DisplayDailyReadings(sensorReadings);
        DisplayMonthlyHealthbar(sensorReadings, 4080);
        */
    }

    void SetupUI() {
        GameObject readingsPanelGO = transform.Find("SensorCanvas/SensorDisplay/ReadingsPanel").gameObject;
        if (readingsPanelGO == null) {
            print("COULDNT FIND READINGSPANEL");
            return;
        } 
        print("FOUND READINGSPANEL");
        CreateGraphs gScript = readingsPanelGO.GetComponent<CreateGraphs>();
        List<float> testList = new List<float>{21f,32f,33f,34f,35f,35f,31f,32f,33f,34f,35f,35f,31f,32f,33f,34f,35f,35f,31f,32f,33f,34f,35f,35f};
        gScript.AddGraph("Temperature", "°C", testList);
        List<float> testList2 = new List<float>{38f,32f,33f,34f,35f,35f,31f,32f,33f,34f,35f,35f,31f,32f,33f,34f,35f,35f,31f,32f,33f,34f,35f,35f};
        gScript.AddGraph("Humidity", "%", testList2);
    }


    void SetupUI_old() {
        GameObject graphPanelGO = transform.Find("SensorCanvas/ReadingsPanel/GraphPanel").gameObject;
        if (graphPanelGO == null) {
            print("COULDNT FIND GRAPHPANEL");
            return;
        } 
        print("FOUND GRAPHPANEL");
        SensorGraph sgScript = graphPanelGO.GetComponent<SensorGraph>();
        List<float> testList = new List<float>{21f,32f,33f,34f,35f,35f,31f,32f,33f,34f,35f,35f,31f,32f,33f,34f,35f,35f,31f,32f,33f,34f,35f,35f};
        sgScript.SetTitle("Temperature");
        sgScript.SetValue("17.74");
        sgScript.SetUnits("°C");
        sgScript.ShowGraph(testList, 45f);
        
    }

    public void HideUI() {
        GameObject lookAt = transform.Find("lookat").gameObject;
        lookAt.SetActive(false);
        GameObject canvas = transform.Find("Canvas").gameObject;
        canvas.SetActive(false);
    }

    public void DisplayUI() {
        GameObject lookAt = transform.Find("lookat").gameObject;
        lookAt.SetActive(true);
        GameObject canvas = transform.Find("Canvas").gameObject;
        canvas.SetActive(true);
    }

}
