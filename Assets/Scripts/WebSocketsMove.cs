using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class WebSocketsMove : MonoBehaviour
{
    string command = null;
	public string webSocketsIP;
	public string webSocketsPort;
    
    // provide access to javascript method
    [DllImport("__Internal")]
    private static extern void WebSocketInit(string url);
    
    // recieve message called from javascript
    void RecieveMessage(string message) {
	    command = message;
    }

    void Start() {
	WebSocketInit("ws://"+webSocketsIP+":"+webSocketsPort+"/");
    print("Trying to connect to ws://"+webSocketsIP+":"+webSocketsPort+"/");
	
    }
    
    void ToggleTestSphere() {
        GameObject farm = GameObject.Find("Farm");
        GameObject sphere = farm.transform.Find("TestSphere").gameObject;
        if (! sphere.active) sphere.SetActive(true);
        else sphere.SetActive(false);
    }


    // Update is called once per frame
    void Update()
    {
        if (command == "click") ToggleTestSphere();
		if (command != null) {
			print("websocket command is "+command);
		}
    }
}
