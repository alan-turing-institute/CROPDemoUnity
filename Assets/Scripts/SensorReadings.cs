using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
using JsonSchema;

// this is attached to the "sensors" GameObject

public class SensorReadings : MonoBehaviour {

    public List<Sensor> sensors;
    public int numSensors = -1; //this will be set by GetSensors - shouldn't be zero,
                                // or we will immediately think we have already retrieved all data
    // increment these counters as we retrieve readings, so we know when we have them all
    int numReadings = 0;
    int numEmpty = 0;
    // base connection string
    public string connection_string;
    // url endpoint for T&RH data
    string trhRequest = "getaranettrhdata/";
    string co2Request = "getaranetco2data/";
    string airVelocityRequest = "getaranetairvelocitydata/";
    string trhSensorType = "Aranet T&RH";
    string co2SensorType = "Aranet CO2";
    string airVelocitySensorType = "Aranet Air Velocity";
    // containers to hold all the readings for all the sensors.
    public Dictionary<int, SensorReadingList> readingsDict;
    bool retrievedAllReadings = false;
    float minTemp = 999f;
    float maxTemp = -999f;
    float minHumid = 999f;
    float maxHumid = -999f;

    public UnityEvent retrievedSensorReadingsEvent = new UnityEvent();

    // Start is called before the first frame update
    void Start()
    {
        sensors = new List<Sensor>();
        readingsDict = new Dictionary<int, SensorReadingList>();
    }

    public void GetSensorReadings() {
       //create current date range (one month from now) query
        string currentDate = System.DateTime.Now.ToString("yyyyMMdd");
        string thirtyDaysAgo = System.DateTime.Now.AddDays(-30).ToString("yyyyMMdd");
        string dateRangeQuery = "?range=" + thirtyDaysAgo + "-" + currentDate;

        foreach (Sensor sensor in sensors) {
            //Get readings of sensor
            if (sensor.sensor_type == trhSensorType) {
                StartCoroutine(GetJsonReadings(connection_string, trhRequest, sensor, dateRangeQuery));
            } else if (sensor.sensor_type == co2SensorType) {
                StartCoroutine(GetJsonReadings(connection_string, co2Request, sensor, dateRangeQuery));
            } else if (sensor.sensor_type == airVelocitySensorType) {
                StartCoroutine(GetJsonReadings(connection_string, airVelocityRequest, sensor, dateRangeQuery));
            } else {
                print("Unknown sensor type "+sensor.sensor_type+" "+sensor.sensor_id);  
            }      
        }
    }

    public IEnumerator GetJsonReadings(string baseURL, string requestEndpoint, Sensor sensor, string dateRange) {
        /*Function to Get sensor readings*/

        //Create a UnityWebRequest for HTTP GET.
        string url = baseURL + "/" + requestEndpoint + sensor.sensor_id.ToString() + dateRange;
        UnityWebRequest webRequest = UnityWebRequest.Get(url);
        yield return webRequest.SendWebRequest();

        if (webRequest.isNetworkError || webRequest.isHttpError) {
            Debug.Log("fail: " + webRequest.error + " " + url);
        } else {
            //request json text
            string rawJson = webRequest.downloadHandler.text;
            if (rawJson != "[]") {
                ProcessJson(rawJson);
            } else {
                // keep track of sensors with no data, so we know when we have retrieved everything
                numEmpty += 1;
            }
        }
        void ProcessJson(string jsonString) {
            //remove invalid characters from json
            jsonString = jsonString.Replace("\n", "").Replace("\r", "");
            //wraps request string in json wrapper since can't deserialize lists
            string jsonStart = "{\"readingList\":";
            string jsonEnd = "}";
            jsonString = jsonStart + jsonString + jsonEnd;
            if (sensor.sensor_type == trhSensorType) {
                TempRelHumReadingList readings = JsonUtility.FromJson<TempRelHumReadingList>(jsonString);
                readingsDict[sensor.sensor_id] = readings;
                print("Added T/RH readings for "+sensor.sensor_id + " "+sensor.aranet_code);
            } else if (sensor.sensor_type == co2SensorType) {
                CO2ReadingList readings = JsonUtility.FromJson<CO2ReadingList>(jsonString);
                readingsDict[sensor.sensor_id] = readings;
                print("Added CO2 readings for "+sensor.sensor_id + " "+sensor.aranet_code);
            } else if (sensor.sensor_type == airVelocitySensorType) {
                AirVelocityReadingList readings = JsonUtility.FromJson<AirVelocityReadingList>(jsonString);
                readingsDict[sensor.sensor_id] = readings;
                print("Added AirVelocity readings for "+sensor.sensor_id + " "+sensor.aranet_code);
            } else {
                print("No current actions implemented for sensor type "+sensor.sensor_type);
            }
            numReadings += 1;
        }
    }
    
    public SensorReadingList GetAllReadingsForSensorId(int sensorId) {
        if (! readingsDict.ContainsKey(sensorId) ) {
            print("SensorID "+sensorId+" not in keys of readingsDict");
            return null;    
        }
        return readingsDict[sensorId];
    }

    public List<List<float> > GetOneDayReadingsForSensorId(int sensorId, string sensorType) {
        SensorReadingList readingList = GetAllReadingsForSensorId(sensorId);
        System.DateTime oneDayAgo = System.DateTime.Now.AddDays(-1);
        List<List<float>> outputList = new List<List<float> >();
        if (sensorType == trhSensorType) {
            List<float> temperatures = new List<float>();
            List<float> humidities = new List<float>();
            // create some dictionaries that we will use to calculate one average value per hour
            Dictionary<int, List<float>> tempDict = new Dictionary<int, List<float>>();
            Dictionary<int, List<float>> humidDict = new Dictionary<int, List<float>>();
            for (int i=-24; i<0; i++) {
                tempDict[i] = new List<float>();
                humidDict[i] = new List<float>();
            }
            // populate dictionary keyed by how many hours in the past,
            // with each value being a list of readings in that hour
            foreach (TemperatureHumidityReading reading in ((TempRelHumReadingList)readingList).readingList) {
                System.DateTime dt = System.Convert.ToDateTime(reading.timestamp);
                if (dt < oneDayAgo) continue;
                for (int i=-24; i<0; i++) {
                    if ((dt > System.DateTime.Now.AddHours(i) ) &&
                        (dt < System.DateTime.Now.AddHours(i+1)) ) {
                            tempDict[i].Add(reading.temperature);
                            humidDict[i].Add(reading.humidity);
                    }
                }
            }
            // now take the average of the values for each hour
            for (int i=-24; i<0; i++) {
                if (tempDict[i].Count > 0) {
                    temperatures.Add(tempDict[i].Average());
                    humidities.Add(humidDict[i].Average());
                } else {
                    temperatures.Add(0f);
                    humidities.Add(0f);
                }
            }
            outputList.Add(temperatures);
            outputList.Add(humidities);
        } else if (sensorType == co2SensorType) {
           List<float> co2 = new List<float>();
            // create some dictionaries that we will use to calculate one average value per hour
            Dictionary<int, List<float>> co2Dict = new Dictionary<int, List<float>>();
            for (int i=-24; i<0; i++) {
                co2Dict[i] = new List<float>();
            }
            // populate dictionary keyed by how many hours in the past,
            // with each value being a list of readings in that hour
            foreach (CO2Reading reading in ((CO2ReadingList)readingList).readingList) {
                System.DateTime dt = System.Convert.ToDateTime(reading.timestamp);
                if (dt < oneDayAgo) continue;
                for (int i=-24; i<0; i++) {
                    if ((dt > System.DateTime.Now.AddHours(i) ) &&
                        (dt < System.DateTime.Now.AddHours(i+1)) ) {
                            co2Dict[i].Add(reading.co2);
                    }
                }
            }
            // now take the average of the values for each hour
            for (int i=-24; i<0; i++) {
                if (co2Dict[i].Count > 0) {
                    co2.Add(co2Dict[i].Average());
                } else {
                    co2.Add(0f);
                }
            }
            outputList.Add(co2);            
        } else if (sensorType == airVelocitySensorType) {
           List<float> airvelocities = new List<float>();
            // create some dictionaries that we will use to calculate one average value per hour
            Dictionary<int, List<float>> avDict = new Dictionary<int, List<float>>();
            for (int i=-24; i<0; i++) {
                avDict[i] = new List<float>();
            }
            // populate dictionary keyed by how many hours in the past,
            // with each value being a list of readings in that hour
            foreach (AirVelocityReading reading in ((AirVelocityReadingList)readingList).readingList) {
                System.DateTime dt = System.Convert.ToDateTime(reading.timestamp);
                if (dt < oneDayAgo) continue;
                for (int i=-24; i<0; i++) {
                    if ((dt > System.DateTime.Now.AddHours(i) ) &&
                        (dt < System.DateTime.Now.AddHours(i+1)) ) {
                            avDict[i].Add(reading.air_velocity);
                    }
                }
            }
            // now take the average of the values for each hour
            for (int i=-24; i<0; i++) {
                if (avDict[i].Count > 0) {
                    airvelocities.Add(avDict[i].Average());
                } else {
                    airvelocities.Add(0f);
                }
            }
            outputList.Add(airvelocities);            
        }
        return outputList;
    }


    public SensorReading GetLatestReadingForSensorId(int sensorId, string sensorType) {
        if (! readingsDict.ContainsKey(sensorId) ) {
            return null;
        }   
        // ok we do have a SensorReading for this sensorId.  Now cast the readingsDict
        if (sensorType == trhSensorType) {
            print("Getting latest reading for "+sensorType+" "+sensorId);
            TempRelHumReadingList readings = (TempRelHumReadingList)readingsDict[sensorId];
            TemperatureHumidityReading reading = readings.readingList[0];
            // keep track of min and max readings, if less than one day ago
            System.DateTime oneDayAgo = System.DateTime.Now.AddDays(-1);
            System.DateTime dt = System.Convert.ToDateTime(reading.timestamp);
            if (dt > oneDayAgo) {
                if (reading.temperature > maxTemp) maxTemp = reading.temperature;
                if (reading.temperature < minTemp) minTemp = reading.temperature;
                if (reading.humidity > maxHumid) maxHumid = reading.humidity;
                if (reading.humidity < minHumid) minHumid = reading.humidity;
            }
            return reading;
        } else if (sensorType == co2SensorType) {
            CO2ReadingList readings = (CO2ReadingList)readingsDict[sensorId];
            return readings.readingList[0];
        } else if (sensorType == airVelocitySensorType) {
           AirVelocityReadingList readings = (AirVelocityReadingList)readingsDict[sensorId];
           return readings.readingList[0];
        } else {
            print("Unknown sensor type "+sensorType);
            return null;
        }
    }

    // The TopMenuButtons.DisplayHeatmap() and CalcHeatmaps.CalculateIntersections() functions 
    // both need to know the minumum and maximum values, in order to set colour scale for legend
    // and heatmap, respectively.
    public List<float> GetMinMaxValues(string tempOrHumid) {
        if (tempOrHumid == "temperature") return new List<float>{minTemp, maxTemp};
        else if (tempOrHumid == "humidity") return new List<float>{minHumid, maxHumid};
        else print("Unknown reading type "+tempOrHumid);
        return new List<float>();
    }

    void Update() {
        // check if we have all the expected sensor readings
        if (! retrievedAllReadings ) {
            if (numReadings+numEmpty == numSensors) {
                retrievedSensorReadingsEvent.Invoke();
                retrievedAllReadings = true;
            }
        }

    }

}
