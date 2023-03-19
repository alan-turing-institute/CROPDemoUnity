using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;

using JsonSchema;

// this is the script that will be attached to the "Farm" GameObject, and 
// be used to toggle what type of data is shown.
// To actually display the data, it will call the methods in the ShelfPropagation
// script for each child GameObject (that script is attached to each column)
public class DisplayCropData : MonoBehaviour
{

    bool showingCropType;
    bool showingHarvestTime;
    LegendGenerator legendGenerator;
    GameObject legendPanel;
    //public GameObject legendCanvas;

    public UnityEvent retrievedCropDataEvent = new UnityEvent();

    string url = "https://cropapptest.azurewebsites.net/queries/batchesinfarm_synthetic";
    

    // make a list of colours which we can use for crops
    List<Color> cropColourList = new List<Color>
    { 
        new Color(1F, 1F, 1F, 0.7F),
        new Color(0.7F, 0.05F, 0.2F, 1),
        new Color(0.2F, 0.75F, 0.3F, 1),
        new Color(0.1F, 0.25F, 0.7F, 1),
        new Color(0.6F, 0.05F, 0.7F, 1),
        new Color(0.45F, 0.55F, 0.1F, 1),
        new Color(0.1F, 0.55F, 0.7F, 1),
        new Color(0.2F, 0.55F, 0.2F, 1),
        new Color(1.0F, 0.25F, 0.05F, 1),
        new Color(0.1F, 0.95F, 0.7F, 1),
        new Color(0.3F, 0.35F, 0.6F, 1),
    };

    // what crops do we have?
    // create a dict, keyed by crop name, and fill it as we parse the cropdata
    // from the API, picking out colours from the above list as we go.
    // This will then define the crop-type colour scale (and legend).
    Dictionary<string, Color> cropColourDict = new Dictionary<string, Color>{
        {"unknown/none", new Color(0.75F, 0.75F, 0.75F, 1)},
        {"red cabbage", new Color(0.75F, 0.15F, 0.15F, 1)},
        {"purple radish", new Color(0.6F, 0.05F, 0.7F, 1)},
        {"garlic chive", new Color(0.2F, 0.75F, 0.3F, 1)},
        {"peashoots", new Color(0.3F, 0.85F, 0.1F, 1)}
    };
    //Dictionary<string, Color> cropColourDict = new Dictionary<string, Color>();
    

    // how many days away from harvest are we?  This dictionary will be used
    // to define colour scale for this (and produce a legend).
    Dictionary<string, Color> harvestColourDict = new Dictionary<string, Color>
    {
        {"7", new Color(0.15F, 0.15F, 0.85F, 1)},
        {"6", new Color(0.2F, 0.25F, 0.75F, 1)},
        {"5", new Color(0.3F, 0.35F, 0.65F, 1)},
        {"4", new Color(0.45F, 0.15F, 0.45F, 1)},
        {"3", new Color(0.65F, 0.05F, 0.35F, 1)},
        {"2", new Color(0.85F, 0.25F, 0.05F, 1)},
        {"1", new Color(1F, 0F, 0F, 1)},
        {"unknown/none", new Color(0.75F, 0.75F, 0.75F, 1)}
     };

    // Dictionary keyed by column name (e.g. "A-24"), with internal dictionary keyed by shelf number
    public Dictionary<string, Dictionary<int, CropData> > cropDataDict = new Dictionary<string, Dictionary<int, CropData> >();

    // list of aisles in the farm
    List<string> aisles = new List<string>{"A","B","C","D","E"};
    List <string> tunnels = new List<string>{"Tunnel3","Tunnel4","Tunnel5","Tunnel6"};
    int numShelves = 4;

    enum ShelfColouring {
        Default,
        CropType,
        HarvestTime
    }


    void Start() {
        showingCropType = true;
        showingHarvestTime = false;
        retrievedCropDataEvent.AddListener(HandleCropData);
       // GameObject legendCanvas = GameObject.Find("Canvas");
       // legendGenerator = legendCanvas.GetComponent<LegendGenerator>();
       // legendPanel = legendCanvas.transform.Find("LegendPanel").gameObject;
       // legendPanel.SetActive(false);
        StartCoroutine(GetCropData(url));
    }

    /// get crop data from API
    public IEnumerator  GetCropData(string url) {
      
        UnityWebRequest webRequest = UnityWebRequest.Get(url);
        yield return webRequest.SendWebRequest();

        //check status of request
        if (webRequest.isNetworkError || webRequest.isHttpError) {
            Debug.Log("fail: " + webRequest.error);
        } else {
            //if connection is succesful, request json text
            string jsonString = webRequest.downloadHandler.text;
            ParseCropJson(jsonString);
        }
    }

    void ParseCropJson(string jsonString) {
        //check: remove invalid characters in json.
        jsonString = jsonString.Replace("\n", "").Replace("\r", "");

        //Unity can not parse json lists as sent by the CROP API, to bypass this, 
        //we wrap the received json in a dictionary.
        string jsonStart = "{\"cropList\":";
        string jsonEnd = "}";
        string json = jsonStart + jsonString + jsonEnd;

        //Deserialise Json
        CropDataList cropList = JsonUtility.FromJson<CropDataList>(json);
        ProcessCropData(cropList);
    }

    void ProcessCropData(CropDataList crops) {
        // first add 'unknown/none' to cropColourDict
       // cropColourDict["unknown/none"] = cropColourList[0];
        print("Found cropList of length "+crops.cropList.Count);
        foreach (CropData cd in crops.cropList) {
           // if (cd.aisle != "B") continue;
            //if (cd.column > 4) continue;
            string columnName = cd.aisle+"-"+cd.column.ToString();
            if (! cropDataDict.ContainsKey(columnName) ) {
                cropDataDict[columnName] = new Dictionary<int, CropData>();
            }
            int shelf = cd.shelf;
            CropData sanitizedCropData = SanitizeCropName(cd);
            cropDataDict[columnName][shelf] = sanitizedCropData;
            string cropType = sanitizedCropData.crop_type_name;
            print("Adding crop data to "+columnName+" "+shelf);
           // if (! cropColourDict.ContainsKey(cropType)) {
            //    // add the next colour from the list
            //    cropColourDict[cropType] = cropColourList[cropColourDict.Count];
           // }
        }
        retrievedCropDataEvent.Invoke();
    }
    // to avoid an unsightly legend, remove the trailing description in parentheses
    // e.g. "(micro)" from some crop names.
    CropData SanitizeCropName(CropData cd) {
        if (cd.crop_type_name.Contains("(")) {
            int bracketIndex = cd.crop_type_name.IndexOf("(");
            string newName = cd.crop_type_name.Substring(0, bracketIndex-1);
            cd.crop_type_name = newName;
        }
        return cd;
    }

    CropData MakeDefaultCropData(string zone, string aisle, int column, int shelf) {
        CropData cropData = new CropData();
        cropData.zone = zone;
        cropData.aisle = aisle;
        cropData.column = column;
        cropData.shelf = shelf;
        cropData.crop_type_name = "unknown/none";
        cropData.number_of_trays = 0;
        cropData.tray_size = 0f;
        cropData.event_time = "unknown/none";
        cropData.expected_harvest_time = "unknown/none";
        return cropData;
    }

    void ColourAllShelves(ShelfColouring colourScheme) {
        //loop over aisles
        foreach (string tunnel in tunnels) {
            GameObject tunnelGameObj = GameObject.Find(tunnel);
            if (tunnelGameObj == null) {
                print("Couldn't find tunnel game object "+tunnel);
                continue;
            }
            // loop over aisles
            foreach (Transform aisleTransform in tunnelGameObj.transform) {
                GameObject aisle = aisleTransform.gameObject;
                ///  NOTE - set the "tag" of the aisle gameobject to be 'aisle' in the unity inspector.
                if (aisle.tag != "aisle") continue;
                GenerateColumns columnScript = aisle.GetComponent<GenerateColumns>();
                if (columnScript == null) continue;
                int nColumns = columnScript.column_count + columnScript.column_count_extension;
                for (int i=1; i<= nColumns; i++) {
                    Transform columnTransform = aisle.transform.Find(aisle.name+"-"+i.ToString());
                    if (columnTransform == null) {
                        print("ColourAllShelves "+colourScheme+": Couldn't find "+aisle.name+"-"+i.ToString() );
                        continue;
                    }
                    GameObject column = columnTransform.gameObject;
                    ShelfPropagation shelfScript = column.GetComponent<ShelfPropagation>();
                    if (shelfScript == null) {
                        print("Couldn't find shelf script for "+column.name);
                        continue;
                    }
                    /// if we're colouring everything green, do that here
                    if (colourScheme == ShelfColouring.Default) {
                        shelfScript.ColourShelvesDefault();
                        continue;
                    }
                    // otherwise, look for the crop data, and create default data if none
                    if (! cropDataDict.ContainsKey(column.name)) {
                        // no data for this column - create dictionary to hold default data
                        cropDataDict[column.name] = new Dictionary<int, CropData>();
                    }
                    // we check each shelf, and add default CropData object if
                    // no existing data.
                    for (int shelf=1; shelf <= numShelves; shelf++) {
                        if (! cropDataDict[column.name].ContainsKey(shelf)) {
                            cropDataDict[column.name][shelf] = MakeDefaultCropData(
                                tunnel, aisle.name, i, shelf
                            );
                        }
                    }
                    // now send the data, plus the colour scales to the ShelfPropagation script.
                    shelfScript.cropDataForColumn = cropDataDict[column.name];
                    shelfScript.cropColourDict = cropColourDict;
                    shelfScript.harvestColourDict = harvestColourDict;
                    if (colourScheme == ShelfColouring.CropType) {
                        shelfScript.ColourShelvesCropType();
                    } else if (colourScheme == ShelfColouring.HarvestTime) {
                        shelfScript.ColourShelvesHarvestTime();
                    }
                }
            }
        }
    }

    void HandleCropData() {
        print("RETRIEVED CROP DATA - will now colour by crop type");
        retrievedCropDataEvent.RemoveListener(HandleCropData);
        ColourAllShelvesCropType();
    }

    void ColourAllShelvesCropType() {
    //    legendPanel.SetActive(true);
    //    legendGenerator.itemDict = cropColourDict;
     //   legendGenerator.DrawLegend("Crop types");   
        ColourAllShelves(ShelfColouring.CropType);
    }

    void ColourAllShelvesHarvestTime() {
        legendPanel.SetActive(true);
        legendGenerator.itemDict = harvestColourDict;
        legendGenerator.DrawLegend("Days-to-harvest");
        ColourAllShelves(ShelfColouring.HarvestTime);
    }

    void ColourAllShelvesDefault() {
        legendGenerator.itemDict = null;
        legendPanel.SetActive(false);
        ColourAllShelves(ShelfColouring.Default);
    }

    public void ToggleShowCropType() {
        if (! showingCropType) {
            ColourAllShelvesCropType();
            showingCropType = true;
            showingHarvestTime = false;
        } else {
            ColourAllShelvesDefault();
            showingCropType = false;
        }
    }

    public void ToggleShowHarvestTime() {
        if (! showingHarvestTime) {
            ColourAllShelvesHarvestTime();
            showingCropType = false;
            showingHarvestTime = true;
        } else {
            ColourAllShelvesDefault();
            showingHarvestTime = false;
        }
    }    
}
