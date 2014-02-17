/**
 * Created with JetBrains WebStorm.
 * Author: Torsten Spieldenner
 * Date: 2/17/14
 * Time: 9:41 AM
 * (c) DFKI 2013
 * http://www.dfki.de
 */

var FIVES = FIVES || {};
FIVES.Plugins = FIVES.Plugins || {};

(function () {
    "use strict";

    var renderable = function () {};

    var r = renderable.prototype;

    r.addMeshForEntity = function(entity) {
        if(entity.meshResource.uri)
            FIVES.Resources.ResourceManager.loadExternalResource(entity, this._addMeshToScene.bind(this));
    };

    r._addMeshToScene = function(meshDocument, entityGuid) {
        var entity = FIVES.Models.EntityRegistry.getEntity(entityGuid);
        this._addMeshDefinitionsToScene(entity, meshDocument);
        this._addXml3dGroupsForMesh(entity, meshDocument);
    };

    r._addMeshDefinitionsToScene = function(entity, meshDocument) {
        var meshDefinitions = $(meshDocument).children("defs");
        $(_xml3dElement).append(meshDefinitions);
        entity.xml3dView.defElement = meshDefinitions[0];
    };

    r._addXml3dGroupsForMesh = function(entity, meshDocument) {
        var meshGroup = $(meshDocument).children("group");
        var entityGroup = this._createParentGroupForEntity(entity);
        entity.xml3dView.groupElement = entityGroup;
        entity.xml3dView.groupElement.setAttribute("visible", entity["meshResource"]["visible"]);
        _xml3dElement.appendChild(entity.xml3dView.groupElement);
        $(entity.xml3dView.groupElement).append(meshGroup);
    };

    r._createParentGroupForEntity = function(entity) {
        var entityGroup = XML3D.createElement("group");
        entityGroup.setAttribute("id", "Entity-" + entity.guid);
        entityGroup.setAttribute("transform", "#transform-" + entity.guid );
        return entityGroup;
    };

    FIVES.Plugins.Renderable = new renderable();

}());

