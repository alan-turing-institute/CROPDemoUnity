using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
using UnityEngine;
using JsonSchema;

// This script is attached to the "sensors" GameObject in cropscene1.
public class GetSensors : MonoBehaviour
{
    /* This module sends a request to the flask CROP app as soon as the 3D model is loaded
    on scene and generates sensors at a given location from the database

    dependencies:
    "sensors" empty Game object in main scene 

    global variables: 
    connection_string: query to get a list of sensors, along with sensor type.
    sensor_gobj: the 3D model prefab which represents the sensor to be instantiated.
    */

    public string connection_string;
    public GameObject sensorPrefab;
    SensorReadings sensorReadingsScript;

    Dictionary<string, Color> sensorColours = new Dictionary<string, Color>{
        {"Aranet T&RH", new Color(0.75F, 0.15F, 0.15F, 1)},
        {"Aranet Air Velocity", new Color(0.75F, 0.7F, 0.05F, 1)},
        {"Aranet CO2", new Color(0.15F, 0.15F, 0.75F, 1)},
    };
   
    // hold a dictionary of Sensor objects, keyed by sensorID
    public Dictionary<int, Sensor> sensorDict = new Dictionary<int, Sensor>();
    // keep track of sensor location strings by zone and by sensorID
    private Dictionary<string, Dictionary<int, string> > sensorLocationsByZone = new Dictionary<string, Dictionary<int, string> >();

    void Start() {
        sensorReadingsScript = GetComponent<SensorReadings>();
        sensorReadingsScript.connection_string = connection_string;
        print("In GetSensors::Start() connection_string is "+connection_string);
        StartCoroutine(GetJson(connection_string+"/getallsensors"));

        GameObject testSensor = GameObject.Find("TestSensor");
        if (testSensor != null ) {
            GameObject sensorMesh = testSensor.transform.Find("SensorMesh").gameObject;
            var meshRenderer = sensorMesh.GetComponent<Renderer>();
            meshRenderer.material.color = sensorColours["Aranet Air Velocity"];
            SensorMethods sensorMethodsScript = testSensor.GetComponent<SensorMethods>();
            Sensor sensor = new Sensor();
            sensor.aisle = "A";
            sensor.column = 1;
            sensor.sensor_id = 28;
            sensor.sensor_type = "Aranet T&RH";
            sensor.shelf = 3;
            sensor.zone = "Tunnel3";
            sensor.aranet_code = "TEST";
            sensorMethodsScript.sensor = sensor;
        }
    }

    public IEnumerator GetJson(string url) {
        /* Function to request json from URL. 
        inputs: 
        url : address of json containing the list of sensors
        */

        //Creates a UnityWebRequest for HTTP GET.
        UnityWebRequest webRequest = UnityWebRequest.Get(url);
        yield return webRequest.SendWebRequest();

        //check status of request
        if (webRequest.isNetworkError || webRequest.isHttpError) {
            Debug.Log("fail: " + webRequest.error);
        } else {
        //if connection is succesful, request json text
        string jsonString = webRequest.downloadHandler.text;
        ParseJson(jsonString);
        }
    }

    void ParseJson(string jsonString) {
        //check: remove invalid characters in json.
        jsonString = jsonString.Replace("\n", "").Replace("\r", "");

        //Unity can not parse json lists as sent by the CROP API, to bypass this, 
        //we wrap the received json in a dictionary.
        string jsonStart = "{\"sensorList\":";
        string jsonEnd = "}";
        string jsonWrapper = jsonStart + jsonString + jsonEnd;

        //Deserialise Json
        SensorList sensors = DeserializeSensors(jsonWrapper);
        //find unique sensor types
        FindSensorTypes(sensors);
    }

    SensorList DeserializeSensors(string json) {
        //serialize json using class from script "Jsonclass"
        SensorList sensorObjects = JsonUtility.FromJson<SensorList>(json);

        //test to check serialization counting the object sensors
        // print("no of objects in json: " + sensorObjects.sensorList.Count);

        return sensorObjects;
    }


    void FindSensorTypes(SensorList sensorObjects)
    {
        //create a list of all existing sensor types and instantiate 
        //a parent GameObject for each sensor type

        List<string> sensorTypeList = new List<string>();

        foreach (var sensor in sensorObjects.sensorList) {
            sensorDict[sensor.sensor_id] = sensor;
            if ((sensor.sensor_type != "Aranet T&RH") &&
                (sensor.sensor_type != "Aranet CO2") &&
                (sensor.sensor_type != "Aranet Air Velocity")) continue;
            if (!sensorTypeList.Contains(sensor.sensor_type)) {
                sensorTypeList.Add(sensor.sensor_type);
                GameObject parentSensorType = new GameObject(sensor.sensor_type);
                parentSensorType.transform.parent = gameObject.transform;
            }
        }
        //instantiate all 3D objects
        InstantiateSensors(sensorObjects);
    }

    void InstantiateSensor(Sensor sensor, string sensorLocation) {
        GameObject newSensor;
        if (sensor.sensor_type == "Aranet CO2") sensorLocation = "A-1-4";
        if (sensor.sensor_type == "Aranet Air Velocity") sensorLocation = "B-1-4";
        print("Instantiating "+sensor.sensor_type+" sensor "+sensor.sensor_id+" at location "+sensorLocation);  
       // Vector3 location = GetGlobalSensorLocation(sensor.zone, sensorLocation, sensor.shelf); 
        Vector3 location = GetGlobalSensorLocation(sensor.zone, sensorLocation, 4);   
        // modify the sensor location so that the sensor is visible in front of the shelf.
        float newY = location.y + 210f;
        float newX = location.x;
        if (sensor.sensor_type == "Aranet CO2") newX += 50f;
        else if (sensor.sensor_type == "Aranet Air Velocity") newX -= 50f;
        float newZ = location.z;
        if ((sensor.aisle == "A")) {
            newZ += 800f;
        } else {
            newZ -= 800f;
        }
        Vector3 modifiedLocation = new Vector3(newX, newY, newZ);
        newSensor = Instantiate(sensorPrefab, modifiedLocation, Quaternion.identity); 
        newSensor.name = "sensor_"+sensor.aranet_code.ToString();
        // set the renderer colour
        GameObject sensorMesh = newSensor.transform.Find("SensorMesh").gameObject;
        var meshRenderer = sensorMesh.GetComponent<Renderer>();
        meshRenderer.material.color = sensorColours[sensor.sensor_type];
        //find parent object
        GameObject parentSensor = GameObject.Find(sensor.sensor_type);
        if (parentSensor == null ) {
            print("Couldnt find parent sensor "+sensor.sensor_type);
            return;
        }
        // set new sensor as child of parent object
        newSensor.transform.parent = parentSensor.transform;
        // We want the SensorCanvas child to be inactive by default, while the sensorMesh
        // should be active.
        //GameObject sensorCanvas = newSensor.transform.Find("SensorCanvas").gameObject;
         // set the sensor's Canvas to scale with screen size
        //UnityEngine.UI.CanvasScaler cs = sensorCanvas.GetComponent<UnityEngine.UI.CanvasScaler>();
        //cs.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        //sensorCanvas.SetActive(false);
        //sensorMesh.SetActive(true);
        //Set sensor properties to instantiated sensor
        SensorMethods sensorMethodsScript = newSensor.GetComponent<SensorMethods>();
        sensorMethodsScript.sensor = sensor;

        return;
    }


    void InstantiateSensors(SensorList sensorObjects) {
        /* This function, instantiates 3D sensor prefabs in scene based on sensor type and 
        location of the sensor and assigns the properties (sensor type, sensor id, location and 
        installation date) to each one*/
        print("In InstantiateSensors, have list of "+sensorObjects.sensorList.Count);
        
        
        // create a dict of sensor locations for passing to the heatmap calculator
        Dictionary<int, string> sensorLocations = new Dictionary<int, string>();
        // create a dict of sensors by type and zone to pass to the main menu panel.
        Dictionary< string , Dictionary<string, List<string> > > sensorZones = new Dictionary<string, Dictionary<string, List<string> > >();
        int numSensors = 0;
        // instantiate the sensor in scene function
        foreach (var sensor in sensorObjects.sensorList) {
            //find location of sensor and create location string
            string sensorLocation = sensor.aisle + 
                "-" + sensor.column +
                "-"  + sensor.shelf;
            string sensorType = sensor.sensor_type;
            //print("sensor location: " + sensorLocation);
            if (! sensorZones.ContainsKey(sensorType)) {
                sensorZones[sensorType] = new Dictionary<string, List<string> >();
            }
            // why can't we have a consistent set of zones ? :(
            string zone = sensor.zone;
            
            // sensorZones is used by the SensorPanel script to toggle on or off sensor displays
            if (! sensorZones[sensorType].ContainsKey(zone)) {
                sensorZones[sensorType][zone] = new List<string>();
            }
            sensorZones[sensorType][zone].Add(sensor.aranet_code.ToString());
            // get the location in 3D space
            
            // instantiate the appropriate type of GameObject
            
            if ((sensor.zone == "Retired") || (sensor.zone == "N/A") || (sensor.zone == "Not applicable") ||
            (sensor.zone == "Unknown") || (sensor.zone == "Not in use") || (sensor.zone == null)) continue;
            if (! sensor.zone.Contains("Tunnel")) continue;
            InstantiateSensor(sensor, sensorLocation);
            
            numSensors += 1;
            // sensorLocations is used by the CalcHeatmaps script.
            // So far, only use T&RH sensors
            if (sensor.sensor_type == "Aranet T&RH") {
                if (! sensorLocationsByZone.ContainsKey(zone)) {
                    sensorLocationsByZone[zone] = new Dictionary<int, string>();
                }
                sensorLocationsByZone[zone][sensor.sensor_id] = sensorLocation;
                // sensorLocations[sensor.sensor_id] = sensorLocation;
            }
            
            sensorReadingsScript.sensors.Add(sensor);
           
        }
        // tell the SensorReadings script to fetch its data, and how many sensors to expect
        sensorReadingsScript.numSensors = numSensors;
        sensorReadingsScript.GetSensorReadings();
        
        // set the dict of sensor ids per zone in the menu panels
    //    GameObject mainMenu = GameObject.Find("MainMenuPanel");
        //   if (mainMenu != null) {
        //      mainMenu.GetComponent<SensorPanels>().sensorZones = sensorZones;
        //     print("Setting sensorZones to "+sensorZones);
        //  }
    }

    // get coordinates in x,y,z space
    Vector3 GetGlobalSensorLocation(string zone, string locationString, int shelf=0) {
    
        if (zone.Contains("Tunnel")) { // a location in the farm
            // find generated object 3D shelf with name = to location given. 
            GameObject sensorShelf = GameObject.Find(locationString);
            if (sensorShelf != null) {
                // get centroid of the shelf object
                Vector3 center = sensorShelf.GetComponent<Renderer>().bounds.center;
                Vector3 location = new Vector3(center.x, center.y + shelf * 100, center.z);
            //     print("Found location for zone "+zone+" : "+locationString+" "+location);
                return location;
            }  else {
                print("sensorShelf not found "+zone+" "+locationString);
                return new Vector3(0,0,0);
            }
        } else if ((zone == "R&D") || (zone == "Propagation")) {
            GameObject baseGameObj = GameObject.Find(zone);
            Vector3 location = baseGameObj.transform.position;
            return location;
        } else {
            // we don't expect any other locations to have sensors.
            print("Unknown zone "+zone);
            return new Vector3(0,0,0);
        }
    }

    public Dictionary<int, string > GetSensorLocationsForZone(string zone) {
        if (sensorLocationsByZone.ContainsKey(zone)) {
            return sensorLocationsByZone[zone];
        } else {
            print("No known sensors for zone "+zone);
            return new Dictionary<int, string>();
        }
    }
}
