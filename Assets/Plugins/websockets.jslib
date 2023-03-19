var plugin = {
    WebSocketInit: function(url)
    {
	var init_url = Pointer_stringify(url);
	window.wsclient = new WebSocket(init_url);
	window.wsclient.onopen = function(event) {
	    console.log("[open] Connection established");
	    window.wsclient.send("Unity");
	};

	window.wsclient.onmessage = function(event) {
	    var received_msg = event.data;
	    // console.log(recieved_msg);
	    SendMessage('Player', 'RecieveMessage', received_msg);	
	};
    }
};
mergeInto(LibraryManager.library, plugin);



