using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Events;
using TMPro;

using JsonSchema;

//  Script to be attached to the "Farm" GameObject.   Provide a ShelfData (as defined in JsonSchema)
//  object to a given shelf upon request.

public class ShelfDataHolder : MonoBehaviour
{
    public Sprite redCabbageImage;
    public Sprite garlicChiveImage;
    public Sprite peashootImage;
    public Sprite purpleRadishImage;
    public Sprite noCropImage;
    //public GameObject infoPanel;
   // public GameObject infoPanelText;

    // assign the "Sensors" GameObject in the inspector, so we can access attached scripts
    public GameObject sensorsGO;
    GetSensors sensorScript;
    SensorReadings sensorReadingsScript;
    DisplayCropData cropDataScript;
    bool haveSensorData = false;
    bool haveCropData = false;
    bool haveNearestSensorData = false;
    bool haveAllShelfData = false;
    string currentlyActiveShelf = null;
    // dictionary of sensor IDs, keyed by shelf location aisle-column-shelf
    Dictionary<string, int> nearestSensorDict = new Dictionary<string, int>();
    // dictionary of shelfData, keyed by shelf location aisle-column-shelf
    Dictionary<string, ShelfData> shelfDataDict = new Dictionary<string, ShelfData>();

    Dictionary<string, Sprite> cropImageDict = new Dictionary<string, Sprite>(); 

    Dictionary<string, Color> cropColourDict = new Dictionary<string, Color>{
        {"unknown/none", new Color(0.75F, 0.75F, 0.75F, 1)},
        {"red_cabbage", new Color(0.75F, 0.15F, 0.15F, 1)},
        {"purple radish", new Color(0.6F, 0.05F, 0.7F, 1)},
        {"garlic chive", new Color(0.2F, 0.75F, 0.3F, 1)},
        {"peashoots", new Color(0.3F, 0.85F, 0.1F, 1)}
    };

    void Start() {
        // notify ourselves when the sensor and crop data has been retrieved by the relevant scripts
        sensorScript = sensorsGO.GetComponent<GetSensors>();
        sensorReadingsScript = sensorsGO.GetComponent<SensorReadings>();
        sensorReadingsScript.retrievedSensorReadingsEvent.AddListener(GotSensorData);
        cropDataScript = GetComponent<DisplayCropData>();
        cropDataScript.retrievedCropDataEvent.AddListener(GotCropData);

        //ResetShelfText();
        string url = "http://cropapptest.azurewebsites.net/queries/closest_trh_sensors";
        StartCoroutine(GetNearestSensorData(url));
        
        // load image textures
        {
        cropImageDict["red_cabbage"] =  redCabbageImage;
        cropImageDict["peashoots"] = peashootImage;
        cropImageDict["garlic chive"] = garlicChiveImage;
        cropImageDict["purple radish"] = purpleRadishImage;
        cropImageDict["unknown/none"] = noCropImage;
    };
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
          //  print("FILLING SHELF DATA FOR "+entry.Key);
            int nearestSensorID = entry.Value;
            sd.sensor_id = nearestSensorID;
            // find the details of the nearest sensor
            sd.nearestSensor = new SensorList();
            sd.nearestSensor.sensorList = new List<Sensor>();
            if (sensorScript.sensorDict.ContainsKey(nearestSensorID)) {
                sd.nearestSensor.sensorList.Add(sensorScript.sensorDict[nearestSensorID]);
            }
           // print("Shelf "+entry.Key+" nearest sensor "+sd.sensor_id);
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
               // print("Trying to get cropData for column "+columnName+" "+shelfID);
                if (shelfCropDict.ContainsKey(shelfID)) {
                    sd.cropData.cropList.Add(shelfCropDict[shelfID]);
                }
            }

            shelfDataDict[entry.Key] = sd;
        }
        // fake shelf data for testing column F
        for (int shelf=1; shelf<5; shelf++) {
            string shelfName = "F-1-"+shelf.ToString();
            ShelfData sd = new ShelfData();
            sd.sensor_id = 27;
            sd.nearestSensor = new SensorList();
            sd.nearestSensor.sensorList = new List<Sensor>();
            if (sensorScript.sensorDict.ContainsKey(sd.sensor_id)) {
                sd.nearestSensor.sensorList.Add(sensorScript.sensorDict[sd.sensor_id]);
            }
            sd.latestReading = new TempRelHumReadingList();
            sd.latestReading.readingList = new List<TemperatureHumidityReading>();
            if (sensorReadingsScript.readingsDict.ContainsKey(sd.sensor_id)) {
                TempRelHumReadingList readings = (TempRelHumReadingList)(sensorReadingsScript.readingsDict[sd.sensor_id]);
                if (readings.readingList.Count > 0) {
                    sd.latestReading.readingList.Add(readings.readingList[0]);
                }
            }
            sd.cropData = new CropDataList();
            sd.cropData.cropList = new List<CropData>();
            // column name will be entry.Key 
            string columnName = "A-1";
            if (cropDataScript.cropDataDict.ContainsKey(columnName)) {
                Dictionary<int, CropData> shelfCropDict = cropDataScript.cropDataDict[columnName];
                int shelfID = shelf;
               // print("Trying to get cropData for column "+columnName+" "+shelfID);
                if (shelfCropDict.ContainsKey(shelfID)) {
                    sd.cropData.cropList.Add(shelfCropDict[shelfID]);
                }
            }
            shelfDataDict[shelfName] = sd;
        }
        print("Have all shelf data");
        haveAllShelfData = true;
    }

    //public void ResetShelfText() {
    //    infoPanelText.GetComponent<Text>().text = "No information to display";
    //}

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

    void SetInfoPanel(GameObject infoPanel, string shelfName) {
        // set the text and images on the info panel.
        GameObject idText = infoPanel.transform.Find("ShelfIDText").gameObject;
        Text t = idText.GetComponent<Text>();
        t.text = shelfName;
        
        ShelfData sd = shelfDataDict[shelfName];
        
        if (sd.cropData.cropList.Count > 0) {
            
            GameObject ctText = infoPanel.transform.Find("CropTypeText").gameObject;
            
            Text ctt = ctText.GetComponent<Text>();
            
            ctt.color = cropColourDict[sd.cropData.cropList[0].crop_type_name];
            ctt.text = sd.cropData.cropList[0].crop_type_name;
            
            GameObject cropImage = infoPanel.transform.Find("CropImage").gameObject;
            Image cii = cropImage.GetComponent<Image>();
            cii.sprite = cropImageDict[sd.cropData.cropList[0].crop_type_name];
            
            GameObject ciText = infoPanel.transform.Find("CropInfoText").gameObject;
            Text cit = ciText.GetComponent<Text>();
            string infotext = "Number of trays: "+sd.cropData.cropList[0].number_of_trays.ToString()+"\n";
            infotext += "Harvest date: "+sd.cropData.cropList[0].expected_harvest_time+"\n";
            cit.text = infotext;
        }
        if (sd.nearestSensor.sensorList.Count > 0) {
            GameObject siText = infoPanel.transform.Find("SensorInfoText").gameObject;
            Text sit = siText.GetComponent<Text>();
            string sensorText = "";
            sensorText += "Nearest T/RH sensor: "+sd.nearestSensor.sensorList[0].aisle.ToString()+sd.nearestSensor.sensorList[0].column.ToString()+sd.nearestSensor.sensorList[0].shelf.ToString()+"\n";
            if (sd.latestReading.readingList.Count > 0) {
                sensorText += "Latest T/RH reading time: "+sd.latestReading.readingList[0].timestamp+"\n";
                sensorText += "Temperature: "+sd.latestReading.readingList[0].temperature.ToString()+" C \n";
                sensorText += "Humidity: "+sd.latestReading.readingList[0].humidity.ToString()+" %";
            }
            sit.text = sensorText;

        }


    }

    public void ShowShelfId(GameObject button) {
        Transform idTrans = button.transform.GetChild(0);
        idTrans.gameObject.SetActive(true);
    }

   public void HideShelfId(GameObject button) {
        Transform idTrans = button.transform.GetChild(0);
        idTrans.gameObject.SetActive(false);
        
    }
/*
    public void ShowAllShelfIds(string columnName) {
        for (int i=1; i<5; i++) {
            string shelfName = columnName+"-"+i.ToString();
            ShowShelfId(shelfName);
        }
    }

    public void HideAllShelfIds(string columnName) {
        for (int i=1; i<5; i++) {
            string shelfName = columnName+"-"+i.ToString();
            HideShelfId(shelfName);
        }
    }
*/
    public void ToggleInfoPanel(GameObject button) {
        string shelfName = button.name.Split("_")[0];
        string columnName = shelfName.Split("-")[0]+"-"+shelfName.Split("-")[1];
        if (currentlyActiveShelf == null) {
            // no info panel currently showing - show this one!
            ShowInfoPanel(shelfName);
            HideShelfId(button);
            currentlyActiveShelf = shelfName;
        } else if (currentlyActiveShelf == shelfName) {
            // this info panel currently showing - hide it!
            HideInfoPanel(shelfName);
            ShowShelfId(button);
            currentlyActiveShelf = null;
        } else {
            // different shelf's info panel being shown - hide that and show this one!
            string oldColumnName = currentlyActiveShelf.Split("-")[0]+"-"+currentlyActiveShelf.Split("-")[1];
            HideInfoPanel(currentlyActiveShelf);
            ShowShelfId(button);
            ShowInfoPanel(shelfName);
            HideShelfId(button);
            currentlyActiveShelf = shelfName;       
        }

        return;
    }

    


    public void ShowInfoPanel(string shelfName) {
        GameObject shelf = GameObject.Find(shelfName);
        int shelfNum = int.Parse(shelfName[shelfName.Length -1].ToString());
        if (shelf != null) {
            Transform columnTransform = shelf.transform.parent;
            GameObject infoCanvas = columnTransform.Find("ColumnCanvas").gameObject;
            Vector3 canvasPosition = infoCanvas.transform.localPosition;
            //now move it up or down a bit depending on shelf
            infoCanvas.transform.localPosition = new Vector3(canvasPosition.x,900+shelfNum*150, canvasPosition.z);
            GameObject infoPanel = columnTransform.Find("ColumnCanvas/InfoPanel").gameObject;
            SetInfoPanel(infoPanel, shelfName);
            infoCanvas.SetActive(true);
        }       
    }   

    public void HideInfoPanel(string shelfName) {   
        GameObject shelf = GameObject.Find(shelfName);
        if (shelf != null) {
            Transform columnTransform = shelf.transform.parent;
            GameObject infoCanvas = columnTransform.Find("ColumnCanvas").gameObject;
            infoCanvas.SetActive(false);
        }       
    }   

    void Update() {
        //print("DO I HAVE ALL THE DATA? "+haveAllShelfData+" "+haveCropData+" "+haveSensorData+" "+haveNearestSensorData);
        if ((! haveAllShelfData) && haveCropData && haveSensorData && haveNearestSensorData) {
            FillShelfData();
        }
    }
}
