/**
 * Created with JetBrains WebStorm.
 * Author: Torsten Spieldenner
 * Date: 1/16/14
 * Time: 1:35 PM
 * (c) DFKI 2013
 * http://www.dfki.de
 */

var FIVES = FIVES || {};
FIVES.WebclientTestsuite = FIVES.WebclientTestsuite || {};

(function(){

    "use strict";
    var PLOT_HEIGHT = 150;

    var timestampReferenceDate = new Date(2013, 11, 4, 19, 50, 0).getTime();

    var testConnection = function(n) {

        this.RoundtripDelays = [];
        this.DelaysToAttribute = [];
        this.QueueProcessing = [];
        this.receivedMessages = 0;

        this.clientNumber = n;
        this.createPlaceholderDiv(n);

        this.communicator = new FIVES.WebclientTestsuite.FivesCommunicator(this);
        this.communicator.initialize();

        var self = this;

        this.communicator.auth(n.toString(), "", function(){
            self.communicator.connect(self.createUpdateEntity.bind(self));
        });
    };

    var c = testConnection.prototype;

    var _generateTimestamp = function() {
        var updateTime = new Date().getTime();
        var timeStamp = updateTime - timestampReferenceDate;
        return timeStamp;
    };

    c.createPlaceholderDiv = function(i) {
        var positionY = 50 + (i-1) * PLOT_HEIGHT;
        this.placeholderID = "plot" + i;

        var placeholderDiv = $('<div id="' + this.placeholderID + '"' +
            ' style=" position: absolute; width: 1500px; height: ' + PLOT_HEIGHT + 'px;' +
            'top: ' + positionY +'px;" >');
        $("body").append(placeholderDiv);
    };

    c.createUpdateEntity = function() {
        var createEntityRequest = this.communicator.createEntityAt({x: _generateTimestamp(), y: 0, z: 0});
        createEntityRequest.on("success", this.initializeSimulation.bind(this));
    };


    c.initializeSimulation = function(createdEntityGuid) {
        this.createdEntityGuid = createdEntityGuid;
        this._updateIntervalHandler = window.setInterval(this.updateEntityPosition.bind(this),
            FIVES.WebclientTestsuite.MESSAGE_INTERVAL_MS);

        var self = this;
        window.setTimeout(this.finishExperiment.bind(this), FIVES.WebclientTestsuite.EXPERIMENT_RUNTIME_S * 1000);
    };

    c.updateEntityPosition = function() {
        this.communicator.updateEntityPosition(this.createdEntityGuid,
            {x: _generateTimestamp(), // Current timestamp to measure time the message was actually sent
             y: 0, // y and z values will be set during server side processing
             z: 0},
            0);
    };

    c.handleUpdate = function(handledUpdate) {
        this.receivedMessages ++;
        if(handledUpdate.entityGuid == this.createdEntityGuid)
        {
            if(handledUpdate.componentName == "position")
            {
                switch(handledUpdate.attributeName)
                {
                    case 'x': this.RoundtripDelays.push([this.RoundtripDelays.length,
                        _generateTimestamp() - handledUpdate.value]);
                        break;
                    case 'y': this.DelaysToAttribute.push([this.DelaysToAttribute.length,
                        handledUpdate.value + 9]); // 9 here is estimated difference to server
                        break;
                    case 'z': this.QueueProcessing.push([this.QueueProcessing.length,
                        handledUpdate.value]);
                        break;
                }
            }
        }
    };

    c.finishExperiment = function() {
        window.clearInterval(this._updateIntervalHandler);
        var roundtripAv = this.getAverage(this.RoundtripDelays);
        $.plot($("#" + this.placeholderID),[
            { label: "Client " + this.clientNumber, data: this.RoundtripDelays, lines: {show: true, fill: true}},
            { label: "Incoming Proc.", data: this.DelaysToAttribute, lines: {show: true}},
            { label: "Queue time", data: this.QueueProcessing, lines: {show: true}},
            { label: "Avg: " + roundtripAv, data: [[0, roundtripAv], [this.RoundtripDelays.length, roundtripAv]],
                lines: {show: true}}
        ], {yaxis: {max: roundtripAv*4}});

        FIVES.WebclientTestsuite.CollectResults(this.receivedMessages);
    };

    c.getAverage = function(measurement) {
        var accumulatedValue = 0;
        for(var i = 0; i < measurement.length; i++) {
            accumulatedValue += measurement[i][1];
        }
        return (accumulatedValue / measurement.length);
    };

    FIVES.WebclientTestsuite.testConnection = testConnection;

}());
