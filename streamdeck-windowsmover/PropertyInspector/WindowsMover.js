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
    setLocationFilterWrapper("none");
    setTitleFilterWrapper("none");
    setApplicationChooser("none");

    if (payload['resizeWindow']) {
        setHeightWidthWrapper("");
    }

    if (payload['filterLocation']) {
        setLocationFilterWrapper("");
    }

    if (payload['filterTitle']) {
        setTitleFilterWrapper("");
    }

    if (payload['appSpecific']) {
        setApplicationChooser("");
    }
}

function setHeightWidthWrapper(displayValue) {
    var dvHeight = document.getElementById('dvHeight');
    var dvWidth = document.getElementById('dvWidth');
    dvHeight.style.display = displayValue;
    dvWidth.style.display = displayValue;
}

function setLocationFilterWrapper(displayValue) {
    var dvLocation = document.getElementById('dvLocationFilter');
    dvLocation.style.display = displayValue;
}

function setTitleFilterWrapper(displayValue) {
    var dvTitle = document.getElementById('dvTitleFilter');
    dvTitle.style.display = displayValue;
}

function setApplicationChooser(displayValue) {
    var dvAppSpecific = document.getElementById('dvAppSpecific');
    var dvAppFilters = document.getElementById('dvAppFilters');
    dvAppSpecific.style.display = displayValue;
    dvAppFilters.style.display = displayValue;
}

function getWindowDetails() {
    var payload = {};
    payload.property_inspector = 'getWindowDetails';
    sendPayloadToPlugin(payload);
}