using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResetPosition : MonoBehaviour
{
    Vector3 initialPosition;
    Quaternion initialRotation;

    // Start is called before the first frame update
    void Start()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        
    }

    // Update is called once per frame
    public void Reset()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
    }
}
