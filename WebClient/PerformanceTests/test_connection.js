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

    var today = new Date();
    var timestampReferenceDate = new Date(today.getFullYear(), today.getMonth(), today.getDate(), 0, 0, 0).getTime();

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
        var positionY = 105 + (i-1) * PLOT_HEIGHT;
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
                        handledUpdate.value ]);
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

        FIVES.WebclientTestsuite.CollectResults(this.receivedMessages,
            this.RoundtripDelays,
            this.DelaysToAttribute,
            this.QueueProcessing);
    };

    FIVES.WebclientTestsuite.testConnection = testConnection;

}());
