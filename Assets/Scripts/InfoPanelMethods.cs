using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfoPanelMethods : MonoBehaviour
{
    public Texture2D infoTexture;
    public Texture2D infoSelectedTexture;
    public float rotationSpeed = 10;
    public GameObject panelMeshHolder;
    public GameObject panelMeshFront;
    public GameObject panelMeshBack;
    public GameObject panelCanvas;

    // Start is called before the first frame update
    void Start() {
        SetSelectedTexture(false);
        OnSelectExit();
    }
    void SetSelectedTexture(bool isHighlighted) {
        GameObject[] meshes = {panelMeshFront, panelMeshBack};
        foreach (GameObject panelMesh in meshes) {
            Renderer rend = panelMesh.GetComponent<Renderer>(); 
            if (isHighlighted) {
                rend.material.SetTexture("_MainTex",infoSelectedTexture);
            } else {
                rend.material.SetTexture("_MainTex",infoTexture);
            }
        }
    }

    public void OnHover() {
        SetSelectedTexture(true);
    }

    public void OnHoverExit() {
        SetSelectedTexture(false);   
    }

    public void OnSelect() {
        panelCanvas.SetActive(true);
    }

    public void OnSelectExit() {
        panelCanvas.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        panelMeshHolder.transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);
    }
}
