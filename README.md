# CROPDemoUnity
3D Unity model of the Zero Carbon Farms / Growing Underground farm in Clapham, London.
This is a simplified and modified version of the model in [https://github.com/alan-turing-institute/CROP_unity].  Notable differences are:
* The "big" model of the farm, with the full geometry of the tunnels, lobbies, access shafts, staircases etc. is not included.   Instead, simplified meshes are used for the tunnels, lobbies, and lift shaft.
* Many UI elements, including the weather widget, the buttons for viewing different zones of the farm, selecting the sensors etc. are not included.
* Temperature/humidity heatmaps are not included.
* Only first-person camera view is implemented.

There are two branches under development:
* "VR" is intended for Virtual Reality, uses the XR Interaction Toolkit, and can be deployed on the Meta (Oculus) Quest 2 headset.
* "BigScreen" is intended for having the viewpoint being controlled by a separate app, with communication handled via WebSockets.
