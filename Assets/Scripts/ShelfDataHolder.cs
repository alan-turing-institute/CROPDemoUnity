using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Events;

using JsonSchema;

//  Script to be attached to the "Farm" GameObject.   Provide a ShelfData (as defined in JsonSchema)
//  object to a given shelf upon request

public class ShelfDataHolder : MonoBehaviour
{
    public GameObject infoPanel;
    public GameObject infoPanelText;

    // assign the "Sensors" GameObject in the inspector, so we can access attached scripts
    public GameObject sensorsGO;
    GetSensors sensorScript;
    SensorReadings sensorReadingsScript;
    DisplayCropData cropDataScript;
    bool haveSensorData = false;
    bool haveCropData = false;
    bool haveNearestSensorData = false;
    bool haveAllShelfData = false;
    // dictionary of sensor IDs, keyed by shelf location aisle-column-shelf
    Dictionary<string, int> nearestSensorDict = new Dictionary<string, int>();
    // dictionary of shelfData, keyed by shelf location aisle-column-shelf
    Dictionary<string, ShelfData> shelfDataDict = new Dictionary<string, ShelfData>();

    void Start() {
        // notify ourselves when the sensor and crop data has been retrieved by the relevant scripts
        sensorScript = sensorsGO.GetComponent<GetSensors>();
        sensorReadingsScript = sensorsGO.GetComponent<SensorReadings>();
        sensorReadingsScript.retrievedSensorReadingsEvent.AddListener(GotSensorData);
        cropDataScript = GetComponent<DisplayCropData>();
        cropDataScript.retrievedCropDataEvent.AddListener(GotCropData);

        ResetShelfText();
        string url = "http://localhost:5000/queries/closest_trh_sensors";
        StartCoroutine(GetNearestSensorData(url));
        
    }

    void GotSensorData() {
        haveSensorData = true;
        sensorReadingsScript.retrievedSensorReadingsEvent.RemoveListener(GotSensorData);
    }

    void GotCropData() {
        haveCropData = true;
        cropDataScript.retrievedCropDataEvent.RemoveListener(GotCropData);
    }

    /// get shelf data from API
    public IEnumerator  GetNearestSensorData(string url) {
      
        UnityWebRequest webRequest = UnityWebRequest.Get(url);
        yield return webRequest.SendWebRequest();

        //check status of request
        if (webRequest.isNetworkError || webRequest.isHttpError) {
            Debug.Log("fail: " + webRequest.error);
        } else {
            //if connection is succesful, request json text
            string jsonString = webRequest.downloadHandler.text;
            ParseNearestSensorJson(jsonString);
        }
    }

    void ParseNearestSensorJson(string jsonString) {
        //check: remove invalid characters in json.
        jsonString = jsonString.Replace("\n", "").Replace("\r", "");

        //Unity can not parse json lists as sent by the CROP API, to bypass this, 
        //we wrap the received json in a dictionary.
        string jsonStart = "{\"mappingList\":";
        string jsonEnd = "}";
        string json = jsonStart + jsonString + jsonEnd;

        //Deserialise Json
        NearestSensorMappingList mappingList = JsonUtility.FromJson<NearestSensorMappingList>(json);
        ProcessNearestSensorData(mappingList);
    }

    void ProcessNearestSensorData(NearestSensorMappingList mappingData) {
        
        print("Found shelf-to-sensor mapping of length "+mappingData.mappingList.Count);
        foreach (NearestSensorMapping ns in mappingData.mappingList) {
            string locationName = ns.aisle+"-"+ns.column.ToString()+"-"+ns.shelf.ToString();
            nearestSensorDict[locationName] = ns.sensor_id;
        }
        haveNearestSensorData = true;
    }

    void FillShelfData() {
        print("FILLING SHELF DATA!!");
        // loop through the dictionary of mappings between shelves and nearest sensor
        foreach (KeyValuePair<string, int> entry in nearestSensorDict) {
            ShelfData sd = new ShelfData();
            LocationMaker lm = new LocationMaker();
            sd.location = lm.LocationFromString(entry.Key);
            print("FILLING SHELF DATA FOR "+entry.Key);
            int nearestSensorID = entry.Value;
            sd.sensor_id = nearestSensorID;
            // find the details of the nearest sensor
            sd.nearestSensor = new SensorList();
            sd.nearestSensor.sensorList = new List<Sensor>();
            if (sensorScript.sensorDict.ContainsKey(nearestSensorID)) {
                sd.nearestSensor.sensorList.Add(sensorScript.sensorDict[nearestSensorID]);
            }
            // find the latest sensor reading for the nearest sensor
            sd.latestReading = new TempRelHumReadingList();
            sd.latestReading.readingList = new List<TemperatureHumidityReading>();
            if (sensorReadingsScript.readingsDict.ContainsKey(nearestSensorID)) {
                TempRelHumReadingList readings = (TempRelHumReadingList)(sensorReadingsScript.readingsDict[nearestSensorID]);
                if (readings.readingList.Count > 0) {
                    sd.latestReading.readingList.Add(readings.readingList[0]);
                }
            }
            // add any crop data that we have for this shelf
            sd.cropData = new CropDataList();
            sd.cropData.cropList = new List<CropData>();
            // column name will be entry.Key 
            string columnName = lm.ColumnNameFromLocation(sd.location);
            if (cropDataScript.cropDataDict.ContainsKey(columnName)) {
                Dictionary<int, CropData> shelfCropDict = cropDataScript.cropDataDict[columnName];
                int shelfID = sd.location.shelf;
                print("Trying to get cropData for column "+columnName+" "+shelfID);
                if (shelfCropDict.ContainsKey(shelfID)) {
                    sd.cropData.cropList.Add(shelfCropDict[shelfID]);
                }
            }

            shelfDataDict[entry.Key] = sd;
        }
        haveAllShelfData = true;
    }

    public void ResetShelfText() {
        infoPanelText.GetComponent<Text>().text = "No information to display";
    }

    string ShelfDataToText(ShelfData sd) {
        string text = "\n";
        // the cropData, nearestSensor, and latestReading data should always be a list 
        // of either length 0 (if no data) or length 1.
       // print("UPDATING SHELFDATA FOR "+sd.location.aisle+"-"+sd.location.column+"-"+sd.location.shelf);
        if (sd.cropData.cropList.Count > 0) {
            text += "Current crop: "+sd.cropData.cropList[0].crop_type_name+"\n";
            text += "Number of trays: "+sd.cropData.cropList[0].number_of_trays.ToString()+"\n";
            text += "Expected harvest date: "+sd.cropData.cropList[0].expected_harvest_time+"\n";
        }
        if (sd.nearestSensor.sensorList.Count > 0) {
            text += "Nearest T/RH sensor location: "+sd.nearestSensor.sensorList[0].aisle.ToString()+sd.nearestSensor.sensorList[0].column.ToString()+sd.nearestSensor.sensorList[0].shelf.ToString()+"\n";
            text += "Nearest T/RH sensor ID: "+sd.nearestSensor.sensorList[0].aranet_code + "\n";
        }
        if (sd.latestReading.readingList.Count > 0) {
            text += "Latest T/RH reading time: "+sd.latestReading.readingList[0].timestamp+"\n";
            text += "Latest temperature value: "+sd.latestReading.readingList[0].temperature.ToString()+"\n";
            text += "Latest humidity value: "+sd.latestReading.readingList[0].humidity.ToString()+"\n";
        }
        return text;
    }

    public void UpdateInfoPanel(string shelfID) {
        string text = "Shelf "+shelfID+"\n";
        if (shelfDataDict.ContainsKey(shelfID)) {
            text += ShelfDataToText(shelfDataDict[shelfID]);
        }
        infoPanelText.GetComponent<Text>().text = text;
        return;
    }

    public void ToggleInfoPanel(bool pointingAtSomething=false) {
        // only show the info panel if the cursor is over an object
      //  print("Toggling the info panel");
        if (pointingAtSomething && (! infoPanel.active)) {
            infoPanel.SetActive(true);
        } else {
            infoPanel.SetActive(false);
            ResetShelfText();
        }
    }

    public void ShowInfoPanel(bool pointingAtSomething=false) {
        if (pointingAtSomething &&(! infoPanel.active)) {
            infoPanel.SetActive(true);
        }
    }

    public void HideInfoPanel() {   
        if ( infoPanel.active) {
            infoPanel.SetActive(false);
            ResetShelfText();
        }
    }

    void Update() {
        //print("DO I HAVE ALL THE DATA? "+haveAllShelfData+" "+haveCropData+" "+haveSensorData+" "+haveNearestSensorData);
        if ((! haveAllShelfData) && haveCropData && haveSensorData && haveNearestSensorData) {
            FillShelfData();
        }
    }
}
