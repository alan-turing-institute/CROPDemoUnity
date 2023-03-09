using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using JsonSchema;
using System;

public class SensorMethods : MonoBehaviour
{
    /* This module is attached to the sensor prefab found in the prefabs folder. 
    the moment of the instantiation, it asks the SensorReadings script to send a 
    request to the flask CROP app with the sensor id 
    and get the current readings.
    */
    public GameObject graphPrefab;

    //class to store the basic sensor data.  Provided by GetSensor when sensor is instantiated.
    public Sensor sensor;
    //all the readings for this sensor
    SensorReadingList sensorReadings;

    // use the camera position/direction to decide when to show UI
    GameObject camera;
    public float cameraRange = 1000f;
    public float cameraAngleRange = 30f;
    public bool beingLookedAt = false;

    //objects within sensor prefab that may be disabled or enabled.
    GameObject sensorMesh;
    GameObject sensorCanvas;
    GameObject readingsPanel;
    GameObject noDataPanel;
    GameObject healthBar;
    GameObject sensorId;
    GameObject sensorType;

    string trhSensorType = "Aranet T&RH";
    string co2SensorType = "Aranet CO2";
    string airVelocitySensorType = "Aranet Air Velocity";

    CreateGraphs createGraphsScript;
    SensorReadings sensorReadingsScript;

    // remember the starting colour so we can change with mouseover
    private Color startColour;
    // keep track of what we're showing
    bool activeUI = false;
    bool activeGraphs = false;
    bool hasRecentData = false;

    int numDataPoints = -1; // so we can tell when we have first read the data

    void Start() {
        //Sets up the visualisations of the sensors

        // get the script that holds the readings
        GameObject sensorsGameObj = GameObject.Find("Sensors");
        if (sensorsGameObj == null) {
            print("Couldn't find 'Sensors' GameObject");
            return;
        }
        sensorReadingsScript = sensorsGameObj.GetComponent<SensorReadings>();
        // find the mesh and canvas GameObjects
        sensorMesh = transform.Find("SensorMesh").gameObject;
        sensorCanvas = transform.Find("SensorCanvas").gameObject;
        // find the panel that will contain the graphs
        readingsPanel = transform.Find("SensorCanvas/SensorDisplay/ReadingsPanel").gameObject;
        // the panel that will display if there is no data
        noDataPanel = transform.Find("SensorCanvas/SensorDisplay/NoDataPanel").gameObject;
        //stores the original color so that it can go back to original after mouse over. 
        startColour = sensorMesh.GetComponent<Renderer>().material.color;
        // set the text for sensor type and id 
        sensorType = sensorCanvas.transform.Find("SensorDisplay/SensorType").gameObject;
        sensorId = sensorCanvas.transform.Find("SensorDisplay/SensorID").gameObject;
        healthBar = sensorCanvas.transform.Find("SensorDisplay/HealthBar").gameObject;
        // find the camera GameObject
        camera = GameObject.Find("PlayerCamera");
        // get the script that will create the graphs
        createGraphsScript = readingsPanel.GetComponent<CreateGraphs>();
        SetupUI();
        //print("In SensorMethods start - length of readings for "+sensor.sensor_id+" is "+sensorReadings.readingList.Count);
        //DisplayCurrentReadings(sensorReadings);
        //DisplayDailyReadings(sensorReadings);
        //DisplayMonthlyHealthbar(sensorReadings, 4080);
    }

    public void SetupUI() {
        Text typeText = sensorType.GetComponent<Text>();
        typeText.text = sensor.sensor_type;
        GameObject sensorIdText = sensorId.transform.Find("SensorIDText").gameObject;
        Text idText = sensorIdText.GetComponent<Text>();
        idText.text = sensor.aranet_code;
        // to start off with, hide graphs and UI
        HideUI();
        HideGraphs();
    }

    public void DisplayNoDataPanel() {
        readingsPanel.SetActive(false);
        noDataPanel.SetActive(true);
    }

    public void HideNoDataPanel() {
        readingsPanel.SetActive(false);
        noDataPanel.SetActive(false);
    }

    public void HideGraphs() {
      //  print("Hiding graphs for "+gameObject.name);
        readingsPanel.SetActive(false);
        noDataPanel.SetActive(false);
        activeGraphs = false;
    }

 

    public void DisplayGraphs() {
        print("Displaying graphs for "+gameObject.name);
        sensorReadings = sensorReadingsScript.GetAllReadingsForSensorId(sensor.sensor_id);
        if (sensorReadings == null) print("Sensor readings for "+sensor.sensor_id+" is NULL!");        
        activeGraphs = true;
        // add graphs if necessary
        readingsPanel.SetActive(true);
        if (readingsPanel.transform.childCount == 0) {
            numDataPoints = AddGraphs();
        }
        if (numDataPoints ==0) DisplayNoDataPanel();
        
        
    }

    public int AddGraphs() {
        List<List<float> > values = sensorReadingsScript.GetOneDayReadingsForSensorId(sensor.sensor_id, sensor.sensor_type);
        if (sensor.sensor_type == trhSensorType) {
            List<float> tvals = values[0];
            List<float> hvals = values[1];
            createGraphsScript.AddGraph("Temperature", "°C", tvals);
            createGraphsScript.AddGraph("Humidity", "%", hvals);
            return tvals.Count;
        } else if (sensor.sensor_type == co2SensorType) {
            List<float> co2vals = values[0];
            createGraphsScript.AddGraph("CO2", "ppm", co2vals);
            return co2vals.Count;
        } else if (sensor.sensor_type == "Aranet Air Velocity") {
            List<float> avvals = values[0];
            createGraphsScript.AddGraph("Air Velocity", "m/s", avvals);
            return avvals.Count;
        } else {
            print("Unknown sensor type "+sensor.sensor_type);
            return 0;
        }
    }

    public void HideUI() {
        sensorId.SetActive(false);
        sensorType.SetActive(false);
        healthBar.SetActive(false);
        activeUI = false;
    }

    public void DisplayUI() {
        sensorId.SetActive(true);
        sensorType.SetActive(true);
        healthBar.SetActive(true); 
        activeUI = true;     
    }
/*
    void DisplayCurrentReadings(SensorReadingList sensorReadings) {
        print("In DisplayCurrentReadings");
        print("sensor_type is "+sensor.sensor_type);
        if (sensor.sensor_type == trhSensorType) {
            TempRelHumReadingList readings = (TempRelHumReadingList)sensorReadings;
            if (readings == null) return;
            int numReadings = readings.readingList.Count;   
            if (numReadings == 0) return;
            // assume that the latest reading is the first in the list
            TemperatureHumidityReading currentReading = readings.readingList[0];
            //set text in panels
            print("Setting trh text in the panel "+currentReading.temperature.ToString()+" "+currentReading.humidity.ToString());
            temp_str_obj.text = currentReading.temperature.ToString();
            humid_str_obj.text = currentReading.humidity.ToString();
        } else if (sensor.sensor_type == co2SensorType) {
            CO2ReadingList readings = (CO2ReadingList)sensorReadings;
            if (readings == null) return;
            int numReadings = readings.readingList.Count;   
            if (numReadings == 0) return;
            // assume that the latest reading is the first in the list
            CO2Reading currentReading = readings.readingList[0];
            //set text in panels
            print("Setting co2 text in the panel "+currentReading.co2.ToString());
            bool textIsNull = (co2_str_obj == null);
            print(" is co2 text null? "+textIsNull);
            co2_str_obj.text = currentReading.co2.ToString();
            print("Finished setting co2 text");
        } else if (sensor.sensor_type == airVelocitySensorType) {
            AirVelocityReadingList readings = (AirVelocityReadingList)sensorReadings;
            if (readings == null) return; 
            int numReadings = readings.readingList.Count;   
            if (numReadings == 0) return;
            // assume that the latest reading is the first in the list
            AirVelocityReading currentReading = readings.readingList[0];
            //set text in panels
            string readingVal = currentReading.air_velocity.ToString("0.00");
            //string readingText = String.Format("0:0.00", readingVal);
            print("Setting airvel text in the panel "+currentReading.air_velocity.ToString());
            airvelocity_str_obj.text = readingVal;
        }
        print("End of DisplayCurrentReadings");
    }

    //get the daily readings for the plots
    void DisplayDailyReadings(SensorReadingList sensorReadings) {
        print("In DisplayDailyReadings");
        System.DateTime startTime = System.DateTime.Now.AddDays(-1);
        List<int> hourList = new List<int>();
        // create dictionary to store values per hour
        Dictionary<int,int> readingCount = new Dictionary<int,int>();

        
        if (sensor.sensor_type == trhSensorType) {
            TempRelHumReadingList readings = (TempRelHumReadingList)sensorReadings;
            Dictionary<int,float> temperatureTotal = new Dictionary<int,float>();
            Dictionary<int,float> humidityTotal = new Dictionary<int,float>();
            //get daily entries from request
            if (readings == null) return;
            foreach (var entry in readings.readingList) {
                //convert dates string to datetime
                System.DateTime dt = System.Convert.ToDateTime(entry.timestamp);
                if (dt < startTime) continue;
                int hour = dt.Hour;
                if (! hourList.Contains(hour)) hourList.Add(hour);
                try {
                    temperatureTotal[hour] += entry.temperature;
                    humidityTotal[hour] += entry.humidity;
                    readingCount[hour] += 1;
                }
                catch (KeyNotFoundException) {
                    temperatureTotal[hour] = 0f;
                    humidityTotal[hour] = 0f;
                    readingCount[hour] = 0;
                }
            }
            //lists to store values per reading
            List<float> tempList = new List<float>();
            List<float> humidityList = new List<float>();
     
            foreach (var hour in hourList) {
                float averageTemp = temperatureTotal[hour] / (readingCount[hour] + 0.001f);
                float averageHum = humidityTotal[hour] / (readingCount[hour] + 0.001f);
                tempList.Add(averageTemp);
                humidityList.Add(averageHum);
            }
            if (hourList.Count > 0) {
                has_recent_data = true;
                temperature_graph.transform.GetChild(0).GetComponent<SensorGraph>().ShowGraph(tempList, 45f);
                humidity_graph.transform.GetChild(0).GetComponent<SensorGraph>().ShowGraph(humidityList, 100f);
            }
        } else if (sensor.sensor_type == co2SensorType) {
            CO2ReadingList readings = (CO2ReadingList)sensorReadings;
            if (readings == null ) return;
            Dictionary<int,float> co2Total = new Dictionary<int,float>();
            //get daily entries from request
            foreach (var entry in readings.readingList) {
                //convert dates string to datetime
                System.DateTime dt = System.Convert.ToDateTime(entry.timestamp);
                if (dt < startTime) continue;
                int hour = dt.Hour;
                if (! hourList.Contains(hour)) hourList.Add(hour);
                try {
                    co2Total[hour] += entry.co2;
                    readingCount[hour] += 1;
                }
                catch (KeyNotFoundException)
                {
                    co2Total[hour] = 0f;
                    readingCount[hour] = 0;
                }
            }
            //lists to store values per reading
            List<float> co2List = new List<float>();
            float maxAverageCO2 = 0f;
            foreach (var hour in hourList) {
                float averageCO2 = co2Total[hour] / (readingCount[hour] + 0.001f);
                if (averageCO2 > maxAverageCO2) maxAverageCO2 = averageCO2;
                co2List.Add(averageCO2);
            }
            print("size of co2List is "+co2List.Count);
            if (hourList.Count > 0) {
                co2_graph.transform.GetChild(0).GetComponent<SensorGraph>().ShowGraph(co2List, maxAverageCO2*1.5f);        
                has_recent_data = true;
            }
        }  else if (sensor.sensor_type == airVelocitySensorType) {
            AirVelocityReadingList readings = (AirVelocityReadingList)sensorReadings;
            if (readings == null) return;
            Dictionary<int,float> airVelocityTotal = new Dictionary<int,float>();
            //get daily entries from request
            foreach (var entry in readings.readingList) {
                //convert dates string to datetime
                System.DateTime dt = System.Convert.ToDateTime(entry.timestamp);
                if (dt < startTime) continue;
                int hour = dt.Hour;
                if (! hourList.Contains(hour)) hourList.Add(hour);
                try {
                    airVelocityTotal[hour] += entry.air_velocity;
                    readingCount[hour] += 1;
                }
                catch (KeyNotFoundException)
                {
                    airVelocityTotal[hour] = 0f;
                    readingCount[hour] = 0;
                }
            }
            //lists to store values per hour
            List<float> airVelocityList = new List<float>();
            float maxAverageAirVelocity = 0f;
            foreach (var hour in hourList) {
                float averageAirVelocity = airVelocityTotal[hour] / (readingCount[hour] + 0.001f);
                if (averageAirVelocity > maxAverageAirVelocity) maxAverageAirVelocity = averageAirVelocity;
                airVelocityList.Add(averageAirVelocity);
            }
            print("size of airVelocityList is "+airVelocityList.Count);
            if (hourList.Count > 0) {
                has_recent_data = true;
                airvelocity_graph.transform.GetChild(0).GetComponent<SensorGraph>().ShowGraph(airVelocityList, maxAverageAirVelocity*1.5f);        
            }
        } else {
            print("Unknown sensor type "+sensor.sensor_type);
        }
        print("At end of DisplayDailyReadings");
    }

    void DisplayMonthlyHealthbar(SensorReadingList sensorReadings, int maxEntriesPerMonth) {
        print("In DisplayMonthlyHealthbar");
        int readingCount = 0;
        // cast into subclass
        if (sensor.sensor_type == trhSensorType) {
            TempRelHumReadingList readings = (TempRelHumReadingList)sensorReadings;
            if (readings == null) readingCount = 0;
            else readingCount = readings.readingList.Count;
        } else if (sensor.sensor_type == co2SensorType) {
            CO2ReadingList readings = (CO2ReadingList)sensorReadings;
            if (readings == null) readingCount = 0;
            else readingCount = readings.readingList.Count;
        } else if (sensor.sensor_type == airVelocitySensorType) {
            AirVelocityReadingList readings = (AirVelocityReadingList)sensorReadings;
            if (readings == null) readingCount = 0;
            else readingCount = readings.readingList.Count;
        } else {
            print("Unknown sensor type "+sensor.sensor_type);
        }
        //get fill bar
        if (readingCount > maxEntriesPerMonth) {
            healthbar.GetComponent<Slider>().value = 1;
        } else {
            healthbar.GetComponent<Slider>().value = readingCount / maxEntriesPerMonth;
        }
        print("At end of DisplayMonthlyHealthbar");
    }
*/
    ////////////Mouse Controls////////////
    void OnMouseOver() {
        //If your mouse hovers over the GameObject with the script attached, output this message
        sensorMesh.GetComponent<Renderer>().material.color = Color.yellow;
    }

    void OnMouseExit() {
        //The mouse is no longer hovering over the GameObject so output this message each frame
        sensorMesh.GetComponent<Renderer>().material.color = startColour;
    }
/*
    void OnMouseDown() {
        // show/hide graphs on sensor click
        print("On mouse down");
        
        if (activeanel == true) {
            nodata_panel_obj.SetActive(false);
            readings_panel_obj.SetActive(false);
            active_panel = false;
        } else {
            if (has_recent_data) {
                readings_panel_obj.SetActive(true);
            } else {
                nodata_panel_obj.SetActive(true);
            }
            active_panel = true;
        }
    }
    
    void Update() {
        float distanceToCamera = Vector3.Distance(camera.transform.position, sensorMesh.transform.position);
        if ((distanceToCamera < cameraRange) && beingLookedAt) {
            if (! activeUI) {
                DisplayUI();
              
            }
        } else if (activeUI) {
            HideUI();
        }
    } 
    */

}
