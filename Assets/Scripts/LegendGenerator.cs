using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


// Attached to LegendCanvas GameObject

public class LegendGenerator : MonoBehaviour
{
    public Dictionary<string, Color>  itemDict;
    public GameObject parentPanel;
    public GameObject itemPrefab;

    public void ClearLegend() {
        // remove existing entries from the legend
        foreach (Transform child in parentPanel.transform) {
            if (child.gameObject.name != "LegendTitle") {
                Destroy(child.gameObject);
            }
        }
    }

    public void DrawLegend(string legendTitle) {
        // clear the previous legend
        ClearLegend();
        parentPanel.SetActive(true);
        // set the text at the top of the legend
        GameObject titleObj = parentPanel.transform.Find("LegendTitle").gameObject;
        titleObj.GetComponent<Text>().text = legendTitle;
        // instantiate a legend_item prefab for each entry in the legend
        int i=0;
        foreach (KeyValuePair<string,Color> legendItem in itemDict) {
            GameObject newItem = Instantiate(itemPrefab);
            newItem.name = legendItem.Key+"_legend_item";
            Image newImg = newItem.GetComponent<Image>();
            newImg.color = legendItem.Value;
            newItem.GetComponent<RectTransform>().SetParent(parentPanel.transform);
            newItem.GetComponent<RectTransform>().localScale = new Vector3(0.45f,0.3f,1f);
            newItem.GetComponent<RectTransform>().anchoredPosition = new Vector3(100, -80-i*40, 0);
            // now the label
            GameObject txtObj = newItem.transform.GetChild(0).gameObject;
            txtObj.name = legendItem.Key+"_label";
            Text newTxt = txtObj.GetComponent<Text>();
            newTxt.text = legendItem.Key;
            newTxt.color = Color.black;
            newTxt.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
            newTxt.fontSize = 12;
            i++;
        }
        // shrink the parentPanel if necessary
        parentPanel.GetComponent<RectTransform>().offsetMin = new Vector2(400, 230 - 40*i);
    }

    public void HideLegend() {
        ClearLegend();
        parentPanel.SetActive(false);
    }
}
