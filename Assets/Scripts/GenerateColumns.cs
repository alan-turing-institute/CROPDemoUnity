using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class GenerateColumns : MonoBehaviour
{
    /* This module generates an array of prefabs of shelves and trays 
    as configured in the *shelves* prefab in the assets folder 
    based on given location of a parent empty Game Object.
    the generated objects follow diretion and rotation of the parent GO. 

    dependencies:
    Farm column empty objects placed in main farm 3D model.
    e.g. A, B within section "Farm 1"

    global variables: 
    column_obj: prefab of instantiated column with trays in "prefab" folder
    column_count: no of columns generated
    Column_width: distance of generated objects
    */

    public GameObject column_obj;
    public int start_offset = 0;
    public int column_count = 24;
    public int column_count_extension = 8;
    public int gap = 600;
    public float column_width = 2000;
    public string aisleName;

    void Start()
    {
        InstantiateTrays();
    }

    void InstantiateTrays()
    {
        for (int i = 0; i < column_count + column_count_extension; i++) {
            //instantiates an array of trays based on a parent object
            GameObject new_column = Instantiate(column_obj, transform.position, transform.rotation);
            //place instantiated object inside parent
            new_column.transform.parent = gameObject.transform;
            //Name instantiated object
            new_column.name = aisleName + "-" + (i + 1).ToString();
            //define direction and location of new instantiated object
            if (i < column_count) { // main part of farm
                new_column.transform.Translate(Vector3.left * (start_offset + (i * column_width)));
                print("New Column position "+new_column.transform.position);
            } else {
                new_column.transform.Translate( Vector3.left * (start_offset + gap + (i * column_width)));
            }
        }
    }
}
