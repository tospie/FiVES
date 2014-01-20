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

    "use strict";

    FIVES.WebclientTestsuite.RoundtripDelays = [];
    FIVES.WebclientTestsuite.DelaysToAttribute = [];
    FIVES.WebclientTestsuite.QueueProcessing = [];

    FIVES.WebclientTestsuite.TestRoundtripTimes = function() {};
    var t = FIVES.WebclientTestsuite.TestRoundtripTimes.prototype;

    FIVES.WebclientTestsuite.InvokeStart = function() {
        var numberOfClients = $("#clients").val();
        var interval = $("#interval").val();
        var runtime = $("#runtime").val();
        FIVES.WebclientTestsuite.StartTests(numberOfClients, interval, runtime);
    };

    FIVES.WebclientTestsuite.StartTests = function(numberOfClients, messageInterval, experimentRuntime) {

        FIVES.WebclientTestsuite.N_TOTAL_CLIENTS = numberOfClients || 20;
        FIVES.WebclientTestsuite.MESSAGE_INTERVAL_MS = messageInterval || 250; // Interval at which messages are sent in milliseconds
        FIVES.WebclientTestsuite.EXPERIMENT_RUNTIME_S = experimentRuntime || 30; // Runtime of experiment in seconds
        startClient(1);
    };

    var messagesHandledInTotal = 0;
    var resultsReceived = 0;

    FIVES.WebclientTestsuite.CollectResults = function(numMessages, roundtripDelays, delaysToAttribute, queueProcessing)
    {
        messagesHandledInTotal += numMessages;
        resultsReceived ++;
        FIVES.WebclientTestsuite.RoundtripDelays.push(roundtripDelays);
        FIVES.WebclientTestsuite.DelaysToAttribute.push(delaysToAttribute);
        FIVES.WebclientTestsuite.QueueProcessing.push(queueProcessing);
        if(resultsReceived == FIVES.WebclientTestsuite.N_TOTAL_CLIENTS)
        {
            var messagesPerMilliSecond = numMessages / (FIVES.WebclientTestsuite.EXPERIMENT_RUNTIME_S * 1000);
            $("#resultText").text("Total number of messages processed: " + numMessages
                + "  (" + messagesPerMilliSecond +" messages /ms )");

            plotResults();
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

    var plotResults = function() {
        for(var i = 1; i <= FIVES.WebclientTestsuite.N_TOTAL_CLIENTS; i++)
        {
            var placeholderID = "plot" + i;
            var roundtripAv = getAverage(FIVES.WebclientTestsuite.RoundtripDelays[i]);
            $.plot($("#" + placeholderID),[
                { label: "Client " + i, data: FIVES.WebclientTestsuite.RoundtripDelays[i], lines: {show: true, fill: true}},
                { label: "Incoming Proc.", data: FIVES.WebclientTestsuite.DelaysToAttribute[i], lines: {show: true}},
                { label: "Queue time", data: FIVES.WebclientTestsuite.QueueProcessing[i], lines: {show: true}},
                { label: "Avg: " + roundtripAv, data: [[0, roundtripAv], [FIVES.WebclientTestsuite.RoundtripDelays[i].length, roundtripAv]],
                    lines: {show: true}}
            ], {yaxis: {max: roundtripAv*4}});
        }
    };

    var getAverage = function(measurement) {
        var accumulatedValue = 0;
        for(var i = 0; i < measurement.length; i++) {
            accumulatedValue += measurement[i][1];
        }
        return (accumulatedValue / measurement.length);
    };

}());
