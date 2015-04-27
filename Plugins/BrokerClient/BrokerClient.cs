// This file is part of FiVES.
//
// FiVES is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// FiVES is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with FiVES.  If not, see <http://www.gnu.org/licenses/>.
using ClientManagerPlugin;
using FIVES;
using KIARA;
using KIARAPlugin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace BrokerClientPlugin
{
    public class BrokerClient
    {
        private static readonly BrokerClient instance = new BrokerClient();

        public event EventHandler<ClientConnectionEventArgs> Connected;

        public static BrokerClient Instance
        {
            get { return instance; }
        }

        private BrokerClient()
        {
            ReadConfig();
        }

        private void ReadConfig()
        {
            string pluginConfigPath = this.GetType().Assembly.Location;
            XmlDocument configDocument = new XmlDocument();
            configDocument.Load(pluginConfigPath + ".config");
            XmlNode settingsNode = configDocument.SelectSingleNode("configuration/brokerHost");
            if(settingsNode != null)
            {
                remoteBrokerURL = settingsNode.Attributes["url"].Value;
                ConnectToRemoteServer(remoteBrokerURL);
            }
        }

        public ServiceWrapper ConnectToRemoteServer(string uri)
        {
            clientService = ServiceFactory.Discover(uri);
            clientService.OnConnected += OnEstablishedConnection;
            return clientService;
        }

        public void AssignOutgoingToIncomingConnection(Guid incomingSession, Guid outgoingSession)
        {
            outgoingSessionIDs[incomingSession] = outgoingSession;
        }

        private void OnEstablishedConnection(Connection connection)
        {
            connection.RegisterFuncImplementation("objectsync.receiveNewObjects",
                (Action<Dictionary<string, object>>)HandleNewEntityAdded);
            connection.RegisterFuncImplementation("objectsync.removeObject",
                (Action<string>)HandleEntityRemoved);
            connection.RegisterFuncImplementation("objectsync.receiveObjectUpdates",
                (Action<Connection, List<UpdateInfo>>)HandleEntitiesUpdated);

            if (Connected != null)
                Connected(this, new ClientConnectionEventArgs(connection));

            var loginRequest = connection["auth.login"]("BC-" + World.Instance.ID.ToString(), " ");
            loginRequest.OnSuccess<bool>(r => OnAuthenticated(connection, r));
        }

        private void OnAuthenticated(Connection connection, bool success)
        {
            if (success)
            {
                string localServerURI = "http://" + KIARAServerManager.Instance.ServerURI
                    + ":" + KIARAServerManager.Instance.ServerPort
                    + KIARAServerManager.Instance.ServerPath;

                if (remoteBrokerURL != null)
                    connection["broker.registerAsWorldServer"](localServerURI);

                var listRequest = connection["objectsync.listObjects"]();
                listRequest.OnSuccess<List<Dictionary<string, object>>>(e  => ReceiveInitialEntities(e));
            }
        }

        private void ReceiveInitialEntities(List<Dictionary<string, object>> initialEntityList)
        {
            foreach (Dictionary<string, object> entity in initialEntityList)
            {
                HandleNewEntityAdded(entity);
            }
        }

        private void HandleNewEntityAdded(Dictionary<string, object> EntityInfo)
        {
            if (EntityInfo["guid"] == null
                || EntityInfo["owner"] == null
                || EntityInfo["owner"].Equals(World.Instance.ID.ToString())
                || World.Instance.ContainsEntity(new Guid((string)EntityInfo["guid"]))
                )
                // Ignore added entity if it is either not correctly assigned to an owner, or when it was created by
                // the same instance, or when the entity was already added in a previous update
                return;

            Entity receivedEntity
                = new Entity(new Guid((string)EntityInfo["guid"]), new Guid((string)EntityInfo["owner"]));
            foreach (KeyValuePair<string, object> entityComponent in EntityInfo)
            {
                string key = entityComponent.Key;
                if (key != "guid" && key != "owner")
                    ApplyComponent(receivedEntity, key, (Dictionary<string, object>)entityComponent.Value);
            }
            World.Instance.Add(receivedEntity);
        }

        private void ApplyComponent(Entity entity, string componentName, Dictionary<string, object> attributes)
        {
            foreach (KeyValuePair<string, object> attribute in attributes)
            {
                entity[componentName][attribute.Key].Suggest(attribute.Value);
            }
        }

        private void HandleEntityRemoved(string entityGuid)
        {
            if(World.Instance.ContainsEntity(new Guid(entityGuid)))
                World.Instance.Remove(World.Instance.FindEntity(entityGuid));
        }

        private void HandleEntitiesUpdated(Connection connection, List<UpdateInfo> ReceivedUpdate)
        {
            string localGuidAsString = World.Instance.ID.ToString();
            foreach (UpdateInfo update in ReceivedUpdate)
            {
                if (!update.entityOwner.Equals(localGuidAsString))
                {
                    var entity = World.Instance.FindEntity(update.entityGuid);
                    var outgoingId =
                        outgoingSessionIDs.ContainsKey(connection.SessionID)
                        ? outgoingSessionIDs[connection.SessionID]
                        : connection.SessionID;

                    entity[update.componentName][update.attributeName].Suggest(update.value, outgoingId);
                }
            }
        }

        private ServiceWrapper clientService;
        private string remoteBrokerURL;
        private Dictionary<Guid, Guid> outgoingSessionIDs = new Dictionary<Guid,Guid>();
    }
}
