document.addEventListener('websocketCreate', function () {
    console.log("Websocket created!");
    checkResize(actionInfo.payload.settings);

    websocket.addEventListener('message', function (event) {
        console.log("Got message event!");

        // Received message from Stream Deck
        var jsonObj = JSON.parse(event.data);

        if (jsonObj.event === 'sendToPropertyInspector') {
            var payload = jsonObj.payload;
            checkResize(payload);
        }
        else if (jsonObj.event === 'didReceiveSettings') {
            var payload = jsonObj.payload;
            checkResize(payload.settings);
        }
    });
});

function checkResize(payload) {
    console.log("Checking Resize Setting");
    setHeightWidthWrapper("none");
    if (payload['resizeWindow']) {
        setHeightWidthWrapper("");
    }
}

function setHeightWidthWrapper(displayValue) {
    var dvHeight = document.getElementById('dvHeight');
    var dvWidth = document.getElementById('dvWidth');
    dvHeight.style.display = displayValue;
    dvWidth.style.display = displayValue;
}