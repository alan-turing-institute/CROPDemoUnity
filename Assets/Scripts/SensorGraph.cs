using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// This script is attached to the "GraphPanel" GameObject within the prefab
public class SensorGraph : MonoBehaviour
{
    private RectTransform graphContainer;
    public List<float> valueList; 
    public float yMax = 45f;
    public float xdivisions = 24; //(24 hours)
    public string title;
    public string units;
    public string value;


    public void SetTitle(string graphTitle) {  
        GameObject graphTitleGO = transform.Find("Title").gameObject;
        Text titleText = graphTitleGO.GetComponent<Text>();
        titleText.text = graphTitle;
    }

    public void SetValue(string value) {  
        GameObject graphValueGO = transform.Find("Value").gameObject;
        Text valueText = graphValueGO.GetComponent<Text>();
        valueText.text = value;
    }

    public void SetUnits(string units) {  
        GameObject graphUnitsGO = transform.Find("Units").gameObject;
        Text unitText = graphUnitsGO.GetComponent<Text>();
        unitText.text = units;
    }


    //create a cirle at each time point in graph
    private GameObject CreateCircle(Vector2 anchoredPosition)
    {
        GameObject gameObject = new GameObject("circle", typeof(Image));
        gameObject.transform.SetParent(graphContainer, false);
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(0, 0);
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        return gameObject;
    }

    //create lines between time points
    private void CreateDotConnection(Vector2 dotPositionA, Vector2 dotPositionB)
    {
        GameObject gameObject = new GameObject("line", typeof(Image));
        gameObject.transform.SetParent(graphContainer, false);
        gameObject.GetComponent<Image>().color = new Color(0.5f, 0.8f, 0.3f, 1);
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        Vector2 dir = (dotPositionB - dotPositionA).normalized;
        float distance = Vector2.Distance(dotPositionA, dotPositionB);
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.sizeDelta = new Vector2(distance, 2f);
        rectTransform.anchoredPosition = dotPositionA;
        float angle = Mathf.Atan2(dotPositionB.y - dotPositionA.y, dotPositionB.x - dotPositionA.x) * 180 / Mathf.PI;
        rectTransform.anchoredPosition = dotPositionA + dir * distance * 0.5f;
        rectTransform.localEulerAngles = new Vector3(0, 0, angle);
    }

    // clear a graph panel
    private void ClearGraphPanel() {
        foreach (Transform child in graphContainer.transform) {
            if ((child.name=="circle") || (child.name=="line")) {
                Destroy(child.gameObject);
            }
        }
    }
    //Instantiate graph in panel
    public void ShowGraph(List<float> valueList, float yMax)
    {
        graphContainer = transform.Find("Graph").GetComponent<RectTransform>();
        ClearGraphPanel();
        float graphHeight = graphContainer.sizeDelta.y;
        float xSize = graphContainer.sizeDelta.x / xdivisions;

        GameObject lastCircleGameObject = null;
        for (int i = 0; i < valueList.Count; i++)
        {
            float xPosition = i * xSize;
            float yPosition = (valueList[i] / yMax) * graphHeight;
            GameObject circleGameObject = CreateCircle(new Vector2(xPosition, yPosition));
            if (lastCircleGameObject != null)
            {
                CreateDotConnection(lastCircleGameObject.GetComponent<RectTransform>().anchoredPosition, circleGameObject.GetComponent<RectTransform>().anchoredPosition);
            }
            lastCircleGameObject = circleGameObject;
        }
    }

}
