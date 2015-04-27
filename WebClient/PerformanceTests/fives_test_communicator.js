/**
 * Created with JetBrains WebStorm.
 * Author: Torsten Spieldenner
 * Date: 9/18/13
 * Time: 9:22 AM
 * (c) DFKI 2013
 * http://www.dfki.de
 */

var FIVES = FIVES || {};
FIVES.WebclientTestsuite = FIVES.WebclientTestsuite || {};

( function(){
    "use strict";

    var FivesCommunicator = function(connection) {
        this.testConnection = connection;
    };
    var c = FivesCommunicator.prototype;

    // Function wrappers for KIARA interface provided by FIVES server
    c.listObjects = function() {};
    c.getObjectLocation = function() {};
    c.createEntityAt = function() {};
    c.createMeshEntity = function() {};
    c.createServerScriptFor = function() {};
    c.notifyAboutNewObjects = function() {};
    c.getObjectMesh = function() {};
    c.updateEntityLocation = function() {};
    c.notifyAboutLocationOfEntityChanged = function() {};


    c.initialize = function() {
        this.context1 = FIVES.WebclientTestsuite.kiaraContext1;
        console.log(FIVES.WebclientTestsuite.kiaraService1);
        this.context1.openConnection(FIVES.WebclientTestsuite.kiaraService1, _onOpenedConnection.bind(this) );
    };

    // Attempts to authenticate. The `callback` is executed as a function with one argument - true if client was
    // authenticated or false if any other error have happened.
    c.auth = function(username, password, callback) {
        var self = this;

        // If connection has not been established yet - check again in 500 milliseconds.
        if (!self.connection) {
            self.onConnected = c.auth.bind(this, username, password, callback);
            return;
        }

        var reportFailure = function(message) {
            console.error(message);
            FIVES.WebclientTestsuite.FAILED_CLIENTS ++;
        };

        var loginCallback = function(result) {
            if (result == "") {
                reportFailure("Invalid user name or password.");
            } else {
                self.sessionKey = result;
                callback(true);
            }
        };

        var implementsCallback = function(result) {
            if (!result[0] || !result[1]) {
                reportFailure("Server does not support authentication service.");
            } else {
                var login = self.connection.generateFuncWrapper("auth.login");
                login(username, password)
                    .on("success", loginCallback)
                    .on("failure", reportFailure.bind(null, "Failed to authenticate."));
            }
        };

        this.implements(["kiara", "auth"])
            .on("success", implementsCallback)
            .on("failure", reportFailure.bind(null, "Failed to request authentication service from the server."));
    }

    // Attempts to connect to the virtual world. Method `auth` must be used prior to this function to authenticate in
    // the virtual world. The `callback` is executed with one argument - true if the client have been successfully
    // connected or false if some error happened.
    c.connect = function(callback) {
        var self = this;

        _createFunctionWrappers.call(self);
        self.connectedTime = new Date().getTime();
        callback();
    }

    var _onOpenedConnection = function(error, conn) {
        this.connection = conn;
        this.implements = conn.generateFuncWrapper("kiara.implements");

        if (this.onConnected)
            this.onConnected();
    };

    var _listObjectsCallback =  function(error, objects) {

    };

    var _objectUpdate = function(receivedObjectUpdates) {
        for(var entry in receivedObjectUpdates) {
            var handledUpdate = receivedObjectUpdates[entry];
            this.testConnection.handleUpdate(handledUpdate);
        }
    }

    c._generateTimestamp = function() {
        var updateTime = new Date().getTime();
        var timeStamp = this.connectedTime - updateTime;
        return timeStamp;
    };

    var _createFunctionWrappers = function(error, supported) {
        this.listObjects = this.connection.generateFuncWrapper("objectsync.listObjects");
        this.createEntityAt = this.connection.generateFuncWrapper("editing.createEntityAt");
        this.invokeRoundtripTest = this.connection.generateFuncWrapper("browserPerformance.invokeRoundtrip");
        // TODO: Employ correct entityregistry here
        //this.notifyAboutNewObjects(this.sessionKey, FIVES.Models.EntityRegistry.addEntityFromServer.bind(FIVES.Models.EntityRegistry));

        this.connection.registerFuncImplementation("objectsync.receiveObjectUpdates",
            null, _objectUpdate.bind(this));
        this.connection.registerFuncImplementation("objectsync.receiveNewObjects",
            null, function(newObject) {});

        // TODO: Check which of these handlers are actually still needed for performance tests
        /*
        this.getAvatarEntityGuid = this.connection.generateFuncWrapper("avatar.getAvatarEntityGuid");
        this.startAvatarMotionInDirection = this.connection.generateFuncWrapper("avatar.startAvatarMotionInDirection");
        this.setAvatarForwardBackwardMotion = this.connection.generateFuncWrapper("avatar.setAvatarForwardBackwardMotion");
        this.setAvatarLeftRightMotion = this.connection.generateFuncWrapper("avatar.setAvatarLeftRightMotion");
        this.setAvatarSpinAroundAxis = this.connection.generateFuncWrapper("avatar.setAvatarSpinAroundAxis");

        this.startServersideAnimation = this.connection.generateFuncWrapper("animation.startServersideAnimation");
        this.stopServersideAnimation = this.connection.generateFuncWrapper("animation.stopServersideAnimation");

        this.startClientsideAnimation = this.connection.generateFuncWrapper("animation.startClientsideAnimation");
        this.stopClientsideAnimation = this.connection.generateFuncWrapper("animation.stopClientsideAnimation");

        this.notifyAboutClientsideAnimationStart = this.connection.generateFuncWrapper("animation.notifyAboutClientsideAnimationStart");
        this.notifyAboutClientsideAnimationStop = this.connection.generateFuncWrapper("animation.notifyAboutClientsideAnimationStop");
        this.notifyAboutClientsideAnimationStart(FIVES.Plugins.Animation.startAnimationPlayback);
        this.notifyAboutClientsideAnimationStop(FIVES.Plugins.Animation.stopAnimationPlayback);
        */
        this.listObjects().on("result", _listObjectsCallback.bind(this));

        /*
        var getEntityGuidCall = this.getAvatarEntityGuid(this.sessionKey);
        getEntityGuidCall.on("success", function(avatarEntityGuid) {
           FIVES.AvatarEntityGuid = avatarEntityGuid;
        });
        */
    };

    // Expose Communicator to namespace. For the test suite, this is no singleton, as we want to create many connections
    // from within the same browser tab
    FIVES.WebclientTestsuite.FivesCommunicator = FivesCommunicator;

}());
