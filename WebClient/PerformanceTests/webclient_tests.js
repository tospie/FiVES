/**
 * Created with JetBrains WebStorm.
 * Author: Torsten Spieldenner
 * Date: 1/15/14
 * Time: 12:16 PM
 * (c) DFKI 2013
 * http://www.dfki.de
 */

var FIVES = FIVES || {};
FIVES.WebclientTestsuite = FIVES.WebclientTestsuite || {};

(function() {

    FIVES.WebclientTestsuite.N_TOTAL_CLIENTS = 10;
    FIVES.WebclientTestsuite.MESSAGE_INTERVAL_MS = 15; // Interval at which messages are sent in milliseconds
    FIVES.WebclientTestsuite.EXPERIMENT_RUNTIME_S = 20; // Runtime of experiment in seconds

    "use strict";
    FIVES.WebclientTestsuite.RoundtripDelays = [];
    FIVES.WebclientTestsuite.DelaysToAttribute = [];
    FIVES.WebclientTestsuite.QueueProcessing = [];

    FIVES.WebclientTestsuite.TestRoundtripTimes = function() {};
    var t = FIVES.WebclientTestsuite.TestRoundtripTimes.prototype;

    FIVES.WebclientTestsuite.StartTests = function() {
        startClient(1);
    };

    var messagesHandledInTotal = 0;
    var resultsReceived = 0;

    FIVES.WebclientTestsuite.CollectResults = function(numMessages)
    {
        messagesHandledInTotal += numMessages;
        resultsReceived ++;
        if(resultsReceived == FIVES.WebclientTestsuite.N_TOTAL_CLIENTS)
        {
            var messagesPerMilliSecond = numMessages / (FIVES.WebclientTestsuite.EXPERIMENT_RUNTIME_S * 1000);
            $("#resultText").text("Total number of messages processed: " + numMessages
                + "  (" + messagesPerMilliSecond +" messages /ms )");
        }
    };

    var startClient = function(i) {
        if(i <= FIVES.WebclientTestsuite.N_TOTAL_CLIENTS)
        {
            console.log("Starting client " + i);
            var c = new FIVES.WebclientTestsuite.testConnection(i);
            window.setTimeout(function(){startClient(++i);}, 500);
        }
    };

}());
