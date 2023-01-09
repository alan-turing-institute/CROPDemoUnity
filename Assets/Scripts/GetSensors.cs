using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;
using UnityEngine;
using JsonSchema;

// This script is attached to the "sensors" GameObject in cropscene1.

namespace GenSensor
{
    
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
        public GameObject sensor_trh;
        public GameObject sensor_co2;
        public GameObject sensor_airvelocity;
        // keep track of sensor location strings by zone and by sensorID
        private Dictionary<string, Dictionary<int, string> > sensorLocationsByZone = new Dictionary<string, Dictionary<int, string> >();

        void Start()
        {
            /* The coroutine is the object within unity that allows to start a near parallel action
            When using StartCoroutine, unity creates a new object of type Coroutine, 
            this object performs some action and then returns a IEnumerator object (or empty)
            Coroutine is a Unity engine class while IEnumerator belongs to the .NET.*/

            //The folowing starts a coroutine to send a request to the connection string
            //once data are received, they are being tagged into the instantiated "sensors". 
            print("In GetSensors::Start() connection_string is "+connection_string);
            StartCoroutine(GetJson(connection_string+"/getallsensors"));

            //excutes given function with 1f delay after start
            //Invoke("instantiate_sensors", 1f);
        }

        public IEnumerator GetJson(string url)
        {
            /* Function to request json from URL. 
            inputs: 
            url : address of json containing the list of sensors
            */

            //Creates a UnityWebRequest for HTTP GET.
            UnityWebRequest webRequest = UnityWebRequest.Get(url);
            yield return webRequest.SendWebRequest();

            //check status of request
            if (webRequest.isNetworkError || webRequest.isHttpError)
            {
                Debug.Log("fail: " + webRequest.error);

            } else {
                //if connection is succesful, request json text
                string jsonString = webRequest.downloadHandler.text;
                ParseJson(jsonString);
            }
        }

        void ParseJson(string jsonString)
        {
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

        SensorList DeserializeSensors(string json)
        {
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

            foreach (var sensor in sensorObjects.sensorList)
            {
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

        void InstantiateSensors(SensorList sensorObjects) {
            /* This function, instantiates 3D sensor prefabs in scene based on sensor type and 
            location of the sensor and assigns the properties (sensor type, sensor id, location and 
            installation date) to each one*/

            SensorReadings sensorReadingsScript = GetComponent<SensorReadings>();
            sensorReadingsScript.connection_string = connection_string;
            // create a dict of sensor locations for passing to the heatmap calculator
            Dictionary<int, string> sensorLocations = new Dictionary<int, string>();
            // create a dict of sensors by type and zone to pass to the main menu panel.
            Dictionary< string , Dictionary<string, List<string> > > sensorZones = new Dictionary<string, Dictionary<string, List<string> > >();

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
                // why can't we have a consistend set of zones ? :(
                string zone = sensor.zone;
                
                // sensorZones is used by the SensorPanel script to toggle on or off sensor displays
                if (! sensorZones[sensorType].ContainsKey(zone)) {
                    sensorZones[sensorType][zone] = new List<string>();
                }
                sensorZones[sensorType][zone].Add(sensor.aranet_code.ToString());
                // get the location in 3D space
                Vector3 location = GetGlobalSensorLocation(zone, sensorLocation, sensor.shelf);
                // instantiate the appropriate type of GameObject
                GameObject newSensor;
                if (sensor.sensor_type == "Aranet T&RH") {
                    newSensor = Instantiate(sensor_trh, location, Quaternion.identity);
                } else if  (sensor.sensor_type == "Aranet CO2") {
                    newSensor = Instantiate(sensor_co2, location, Quaternion.identity);
                } else if (sensor.sensor_type == "Aranet Air Velocity") {
                    newSensor = Instantiate(sensor_airvelocity, location, Quaternion.identity);
                } else continue;
                newSensor.name = "sensor_"+sensor.aranet_code.ToString();
                
                // sensorLocations is used by the CalcHeatmaps script.
                // So far, only use T&RH sensors
                if (sensor.sensor_type == "Aranet T&RH") {
                    if (! sensorLocationsByZone.ContainsKey(zone)) {
                        sensorLocationsByZone[zone] = new Dictionary<int, string>();
                    }
                    sensorLocationsByZone[zone][sensor.sensor_id] = sensorLocation;
                   // sensorLocations[sensor.sensor_id] = sensorLocation;
                }
                //find parent object
                GameObject parentSensor = GameObject.Find(sensor.sensor_type);
                if (parentSensor == null ) {
                    print("Couldnt find parent sensor "+sensor.sensor_type);
                    continue;
                }
                // set new sensor as child of parent object
                newSensor.transform.parent = parentSensor.transform;

                //Set sensor properties to instantiated sensor
                newSensor.GetComponent<SensorMethods>().sensor = sensor;
                sensorReadingsScript.sensors.Add(sensor);
                // set the sensor's Canvas to scale with screen size
                GameObject sensorCanvas = newSensor.transform.Find("Canvas").gameObject;
                UnityEngine.UI.CanvasScaler cs = sensorCanvas.GetComponent<UnityEngine.UI.CanvasScaler>();
                cs.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                // have all sensors hidden when they are first created.
                newSensor.SetActive(false);
            }
            // tell the SensorReadings script to fetch its data
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
}
