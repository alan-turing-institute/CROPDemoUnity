using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class WindowGraphs : MonoBehaviour
{
    [SerializeField] private Sprite circleSprite;
    private RectTransform graphContainer;
    public GameObject bar;
    public List<float> valueList; 
    public List<float> vList;
    public List<float> humidityList;
    //public List<float> co2List;
    public float yMax = 45f;
    public float xdivisions = 24; //(24 hours)
    public string graph_name;


    private void Awake()
    {

        //CreateCircle(new Vector2(100, 100));

    }

    public void setup(List<float> list, float yMax_ext)
    {   /*set up graphs called from sensor methods */
        yMax = yMax_ext;
        graphContainer = transform.Find("graphContainer").GetComponent<RectTransform>();
        ShowGraph(list);
        //print(list[0]);
    }


    //create a cirle to each time point in graph
    private GameObject CreateCircle(Vector2 anchoredPosition)
    {
        GameObject gameObject = new GameObject("circle", typeof(Image));
        gameObject.transform.SetParent(graphContainer, false);
        //gameObject.GetComponent<Image>().sprite = circleSprite;
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = new Vector2(0, 0);
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        return gameObject;
    }

    //create lines between time points
    private void CreatedotConnection(Vector2 dotPositionA, Vector2 dotPositionB)
    {
        GameObject gameObject = new GameObject("line", typeof(Image));
        gameObject.transform.SetParent(graphContainer, false);
        //gameObject.GetComponent<Image>().color = new Color(0.3f, 0.8f, 0.1f, 0.7f);
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

    //not used. 
    private void Createbar(Vector2 anchoredPosition)
    {
        GameObject gameObject = Instantiate(bar);
        gameObject.transform.SetParent(graphContainer, false);
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(anchoredPosition.x, 0);
        bar.transform.GetChild(0).GetComponent<RectTransform>().offsetMax = new Vector2(0, -50 + anchoredPosition.y); // new Vector2(-right, -top);;
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
    private void ShowGraph(List<float> valueList)
    {
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
                CreatedotConnection(lastCircleGameObject.GetComponent<RectTransform>().anchoredPosition, circleGameObject.GetComponent<RectTransform>().anchoredPosition);
            }
            lastCircleGameObject = circleGameObject;
        }
    }

}
