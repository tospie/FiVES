using System;
using KIARA;
using System.Collections.Generic;
using FIVES;
using Location;

namespace ClientManager {

    /// <summary>
    /// Implements a plugin that can be used to communicate with clients using KIARA.
    /// </summary>
    public class ClientManagerPlugin : IPluginInitializer
    {
        #region IPluginInitializer implementation

        public string GetName()
        {
            return "ClientManager";
        }

        public List<string> GetDependencies()
        {
            return new List<string>() { "Location", "Renderable", "Auth" };
        }

        public void Initialize()
        {
            clientService = ServiceFactory.CreateByURI("http://localhost/projects/test-client/kiara/fives.json");
            var authService = ServiceFactory.DiscoverByName("auth", ContextFactory.GetContext("inter-plugin"));
            authService.OnConnected += (connection) => authPlugin = connection;

            RegisterClientService("kiara", false, new Dictionary<string, Delegate>());
            RegisterClientMethod("kiara.implements", false, (Func<List<string>, List<bool>>)Implements);
            RegisterClientMethod("kiara.implements", true, (Func<List<string>, List<bool>>)AuthenticatedImplements);

            RegisterClientService("auth", false, new Dictionary<string, Delegate>());
            clientService.OnNewClient += delegate(Connection connection) {
                connection.RegisterFuncImplementation("auth.login",
                    (Func<string, string, string>) delegate(string login, string password) {
                        Guid sessionKey = authPlugin["authenticate"](login, password).Wait<Guid>();
                        if (sessionKey == Guid.Empty)
                            return "";
                        authenticatedClients[sessionKey] = connection;
                        if (OnAuthenticated != null)
                            OnAuthenticated(sessionKey);
                        foreach (var entry in authenticatedMethods)
                            connection.RegisterFuncImplementation(entry.Key, entry.Value);
                        return sessionKey.ToString();
                    }
                );
            };

            RegisterClientService("objectsync", true, new Dictionary<string, Delegate> {
                {"listObjects", (Func<List<EntityInfo>>) ListObjects},
                {"notifyAboutNewObjects", (Action<Action<EntityInfo>>) NotifyAboutNewObjects},
                {"notifyAboutRemovedObjects", (Action<Action<string>>) NotifyAboutRemovedObjects},
            });

            // DEBUG
//            clientService["scripting.createServerScriptFor"] = (Action<string, string>)createServerScriptFor;
//            clientService.OnNewClient += delegate(Connection connection) {
//                var getAnswer = connection.generateFuncWrapper("getAnswer");
//                getAnswer((Action<int>) delegate(int answer) { Console.WriteLine("The answer is {0}", answer); });
//            };

            var pluginService = ServiceFactory.CreateByName("clientmanager", ContextFactory.GetContext("inter-plugin"));
            pluginService["registerClientMethod"] = (Action<string, bool, Delegate>)RegisterClientMethod;
            pluginService["registerClientService"] =
                (Action<string, bool, Dictionary<string, Delegate>>)RegisterClientService;
            pluginService["notifyWhenAnyClientAuthenticated"] = (Action<Action<Guid>>)NotifyWhenAnyClientAuthenticated;
            pluginService["notifyWhenClientDisconnected"] = (Action<Guid,Action<Guid>>)NotifyWhenClientDisconnected;
        }

        #endregion

        #region Client interface

        private struct EntityInfo {
            public string guid;
            public string meshURI;
            public bool meshVisible;
            public Vector position;
            public Quat orientation;
            public Vector scale;
        }

        private EntityInfo ConstructEntityInfo(Guid elementId)
        {
            var entity = EntityRegistry.Instance.GetEntity(elementId);

            var entityInfo = new EntityInfo();
            entityInfo.guid = entity.Guid.ToString();
            entityInfo.position.x = (float)entity["position"]["x"];
            entityInfo.position.y = (float)entity["position"]["y"];
            entityInfo.position.z = (float)entity["position"]["z"];
            entityInfo.orientation.x = (float)entity["orientation"]["x"];
            entityInfo.orientation.y = (float)entity["orientation"]["y"];
            entityInfo.orientation.z = (float)entity["orientation"]["z"];
            entityInfo.orientation.w = (float)entity["orientation"]["w"];
            entityInfo.scale.x = (float)entity["scale"]["x"];
            entityInfo.scale.y = (float)entity["scale"]["y"];
            entityInfo.scale.z = (float)entity["scale"]["z"];
            entityInfo.meshURI = (string)entity["meshResource"]["uri"];
            entityInfo.meshVisible = (bool)entity["meshResource"]["visible"];
            return entityInfo;
        }

        private void NotifyAboutNewObjects(Action<EntityInfo> callback)
        {
            EntityRegistry.Instance.OnEntityAdded += (sender, e) => callback(ConstructEntityInfo(e.elementId));
        }

        private void NotifyAboutRemovedObjects(Action<string> callback)
        {
            EntityRegistry.Instance.OnEntityRemoved += (sender, e) => callback(e.elementId.ToString());
        }

        private List<string> basicClientServices = new List<string>();
        private List<bool> Implements(List<string> services)
        {
            return services.ConvertAll(basicClientServices.Contains);
        }

        private List<string> authenticatedClientServices = new List<string>();
        private List<bool> AuthenticatedImplements(List<string> services)
        {
            return services.ConvertAll(authenticatedClientServices.Contains);
        }

        private List<EntityInfo> ListObjects()
        {
            var guids = EntityRegistry.Instance.GetAllGUIDs();
            List<EntityInfo> infos = new List<EntityInfo>();
            foreach (var guid in guids)
                infos.Add(ConstructEntityInfo(guid));
            return infos;
        }

        private void CreateServerScriptFor(string guid, string script)
        {
            var entity = EntityRegistry.Instance.GetEntity(guid);
            entity["scripting"]["serverScript"] = script;
        }

        /// <summary>
        /// The client service.
        /// </summary>
        ServiceImpl clientService;

        /// <summary>
        /// Auth plugin service.
        /// </summary>
        Connection authPlugin;

        /// <summary>
        /// List of authenticated clients.
        /// </summary>
        Dictionary<Guid, Connection> authenticatedClients = new Dictionary<Guid, Connection>();

        /// <summary>
        /// Methods that required user to authenticate before they become available.
        /// </summary>
        Dictionary<string, Delegate> authenticatedMethods = new Dictionary<string, Delegate>();

        event Action<Guid> OnAuthenticated;

        #endregion

        #region Plugin interface

        /// <summary>
        /// Registers the client service.
        /// </summary>
        /// <example>
        /// RegisterClientService("editing", new Dictionary<string, Delegate> {
        ///   {"createObject", (Func<Location, MeshData, string>)CreateObject},
        ///   {"deleteObject", (Action<string>)DeleteObject},
        /// };
        /// </example>
        /// <param name="serviceName">Service name.</param>
        /// <param name="methods">Methods (a map from the name to a delegate).</param>
        /// <param name="requireAuthentication">If set to <c>true</c> require clients to authenticate.</param>
        public void RegisterClientService(string serviceName, bool requireAuthentication,
                                          Dictionary<string, Delegate> methods)
        {
            foreach (var method in methods)
                RegisterClientMethod(serviceName + "." + method.Key, requireAuthentication, method.Value);
            if (!requireAuthentication)
                basicClientServices.Add(serviceName);
            authenticatedClientServices.Add(serviceName);
        }

        /// <summary>
        /// Registers the client method.
        /// </summary>
        /// <example>
        /// RegisterClientMethod("login", (Func<string,string,bool>)LoginToServer, false);
        /// </example>
        /// <param name="methodName">Method name.</param>
        /// <param name="handler">Delegate with implementation.</param>
        /// <param name="requireAuthentication">If set to <c>true</c> require clients to authenticate.</param>
        public void RegisterClientMethod(string methodName, bool requireAuthentication, Delegate handler)
        {
            if (requireAuthentication)
                authenticatedMethods[methodName] = handler;
            else
                clientService[methodName] = handler;
        }

        public void NotifyWhenClientDisconnected(Guid secToken, Action<Guid> callback)
        {
            if (!authenticatedClients.ContainsKey(secToken))
                throw new Exception("Client with with given session key {0} is not authenticated.");

            authenticatedClients[secToken].OnClose += (reason) => callback(secToken);
        }

        void NotifyWhenAnyClientAuthenticated(Action<Guid> callback)
        {
            OnAuthenticated += callback;
        }

        #endregion
    }

}