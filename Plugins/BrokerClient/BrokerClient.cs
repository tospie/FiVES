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

        public void ConnectToRemoteServer(string uri)
        {
            clientService = ServiceFactory.Discover(uri);
            clientService.OnConnected += OnEstablishedConnection;
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
                return;
            Entity receivedEntity = new Entity(new Guid((string)EntityInfo["guid"]), new Guid((string)EntityInfo["owner"]));
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
