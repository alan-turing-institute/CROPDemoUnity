﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JsonSchema
{

    // We might want to replace all the classes where we have zone/aisle/column/shelf
    // with this location class, BUT, in fact on the API side it's easier to have a flat
    // structure where we convert the db query result to an array.
    // In the meantime, we use the Location class in ShelfData.
    [System.Serializable]
    public class Location 
    {
        public string zone;
        public string aisle;
        public int column;
        public int shelf;
    }

    public class LocationMaker {
        public Location LocationFromString(string locationString) {
            Location l = new Location();
            string[] elements = locationString.Split('-');
			l.aisle = elements[0];
			l.column = int.Parse(elements[1]);
			l.shelf = int.Parse(elements[2]);
            return l;
        }
        public string LocationStringFromLocation(Location l) {
            return l.aisle+"-"+l.column.ToString()+"-"+l.shelf.ToString();
        }

        public string ColumnNameFromLocation(Location l) {
            return l.aisle+"-"+l.column.ToString();
        }
    }

    [System.Serializable]
    public class Sensor
    {
        public string aisle;
        public int column;
        public string installation_date;
        public int sensor_id;
        public string sensor_type;
        public int shelf;
        public string zone;
        public string aranet_code;
        public string serial_number;
        public string aranet_pro_id;
    }

    [System.Serializable]
    public class SensorList
    {
        public List<Sensor> sensorList;
    }

    [System.Serializable]
    public class SensorReading
    {
        //public int id;
        public int sensor_id;
        public string time_created;
        public string time_updated;
        public string timestamp;
       
    }

    [System.Serializable]
    public class SensorReadingList
    {
        protected List<SensorReading> readingList;
    }

    [System.Serializable]
    public class TemperatureHumidityReading : SensorReading
    {
        public float humidity;
        public float temperature;
       
    }

    [System.Serializable]
    public class TempRelHumReadingList : SensorReadingList
    {
        public List<TemperatureHumidityReading> readingList;
    }

    [System.Serializable]
    public class CO2Reading : SensorReading
    {
        public float co2;
    }

    [System.Serializable]
    public class CO2ReadingList : SensorReadingList
    {
        public List<CO2Reading> readingList;
    }

    [System.Serializable]
    public class AirVelocityReading : SensorReading
    {
        public float air_velocity;
        public float current;
    }

    [System.Serializable]
    public class AirVelocityReadingList : SensorReadingList
    {
        public List<AirVelocityReading> readingList;
    }

    [System.Serializable]
    public class WeatherText
    {
        public int id;
        public string main;
        public string description;
        public string icon;
    }

    [System.Serializable]
    public class WeatherReading
    {   
        public int dt;
        public int sunrise;
        public int sunset;
        public float temp;
        public float feels_like;
        public int pressure;
        public int humidity;
        public float dew_point;
        public float uvi;
        public int clouds;
        public int visibility;
        public float wind_speed;
        public int wind_deg;
        public List<WeatherText> weather;
    }

    [System.Serializable]
    public class WeatherReadingContainer
    {
        public WeatherReading current;
    }

    [System.Serializable]
    public class CropData
    {
        public string zone;
        public string aisle;
        public int column;
        public int shelf;
        public string crop_type_name;
        public int number_of_trays;
        public float tray_size;
        public string event_time;
        public string expected_harvest_time;
    }

    [System.Serializable]
    public class CropDataList
    {
        public List<CropData> cropList = new List<CropData>();
    }

    [System.Serializable]
    public class ShelfData
    {   
        public Location location;
        public int sensor_id;
        public CropDataList cropData;   // these are lists because they might be empty
        public SensorList nearestSensor;
        public TempRelHumReadingList latestReading;
    }

    [System.Serializable]
    public class ShelfDataList
    {
        public List<ShelfData> shelfList = new List<ShelfData>();
    }

    // For each shelf, what is the nearest T/RH sensor
    [System.Serializable]
    public class NearestSensorMapping
    {
        public string aisle;
        public int column;
        public int shelf;
        public int sensor_id;
    }

    [System.Serializable]
    public class NearestSensorMappingList
    {
        public List<NearestSensorMapping> mappingList = new List<NearestSensorMapping>();
    }


    [System.Serializable]
    public class TextureCoords
    {   
        public string location;
        public Vector2 coords;
    }

    [System.Serializable]
    public class TextureCoordsList
    {
        public List<TextureCoords> coordsList = new List<TextureCoords>();
    }

    /// This is super useful from christophfranke123 at 
    // https://answers.unity.com/questions/460727/how-to-serialize-dictionary-with-unity-serializati.html?childToView=809221#answer-809221
    [System.Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<TKey> keys = new List<TKey>();
     
        [SerializeField]
        private List<TValue> values = new List<TValue>();
     
        // save the dictionary to lists
        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach(KeyValuePair<TKey, TValue> pair in this)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }
     
        // load dictionary from lists
        public void OnAfterDeserialize()
        {
            this.Clear();
 
            if(keys.Count != values.Count)
                throw new System.Exception(string.Format("there are {0} keys and {1} values after deserialization. Make sure that both key and value types are serializable."));
 
            for(int i = 0; i < keys.Count; i++)
                this.Add(keys[i], values[i]);
        }
    }

    [System.Serializable]
    public class TextureCoordsDict : SerializableDictionary<string, Vector2>
    {}

    [System.Serializable]
    public class CropDataDict : SerializableDictionary<string, CropData>
    {}

    [System.Serializable]
    public class FlythroughPanelText : SerializableDictionary<string, string>
    {}

}