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

    //class to store the basic sensor data
    public Sensor sensor;
    //all the readings for this sensor
    public SensorReadingList sensorReadings;

    //3D object within sensor prefab
    GameObject child_sensor_obj;
    private string trhSensorType = "Aranet T&RH";
    private string co2SensorType = "Aranet CO2";
    private string airVelocitySensorType = "Aranet Air Velocity";

    //healthbar sensor ID and type UI text
    public Text sensor_id_obj;
    public Text sensor_type_obj;
    //text values
    public Text temp_str_obj;
    public Text humid_str_obj;
    public Text co2_str_obj;
    public Text airvelocity_str_obj;
    private Color startcolor;
    public GameObject readings_panel_obj;
    public GameObject nodata_panel_obj;
    bool active_panel = false;
    bool has_recent_data = false;

    //plots of last 24hrs data:
    public GameObject temperature_graph;
    public GameObject humidity_graph;
    public GameObject co2_graph;
    public GameObject airvelocity_graph;

    //Healthbars from previous month:
    public GameObject healthbar;
    public GameObject manabar;

    void Start() {
        //Sets up the visualisations of the sensors
        SetupUI();

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
    }

    void SetupUI() {
        /*Function to set up the text visual elements of the 3D sensors*/

        //gets gameobject from sensor prefab - TODO clean this up!
        child_sensor_obj = this.transform.GetChild(1).GetChild(0).GetChild(1).gameObject;

        //stores the original color so that it can go back to original after mouse over. 
        startcolor = child_sensor_obj.GetComponent<Renderer>().material.color;

        //Display sensor type and id in UI text based on sensor properties
        //from instantiation:
        sensor_id_obj.text = sensor.aranet_code.ToString();
        sensor_type_obj.text = sensor.sensor_type.ToUpper();
        // start off with the panel inactive.
        readings_panel_obj.SetActive(false);
        nodata_panel_obj.SetActive(false);
        active_panel = false;
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

    ////////////Mouse Controls////////////
    void OnMouseOver() {
        //If your mouse hovers over the GameObject with the script attached, output this message
        child_sensor_obj.GetComponent<Renderer>().material.color = Color.yellow;
    }

    void OnMouseExit() {
        //The mouse is no longer hovering over the GameObject so output this message each frame
        child_sensor_obj.GetComponent<Renderer>().material.color = startcolor;
    }

    void OnMouseDown() {
        /*Show/hide graphs on sensor click*/
        print("On mouse down");
        
        if (active_panel == true) {
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
}
