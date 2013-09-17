using System;
using FIVES;
using System.Collections.Generic;
using KIARA;
using Events;
using V8.Net;
using NLog;

namespace Scripting
{
    /// <summary>
    /// Scripting plugin.
    /// </summary>
    public class ScriptingPlugin : IPluginInitializer
    {
        #region IPluginInitializer implementation

        /// <summary>
        /// Returns the name of the plugin.
        /// </summary>
        /// <returns>The name of the plugin.</returns>
        public string GetName()
        {
            return "Scripting";
        }

        /// <summary>
        /// Returns the list of names of the plugins that this plugin depends on.
        /// </summary>
        /// <returns>The list of names of the plugins that this plugin depends on.</returns>
        public List<string> GetDependencies()
        {
            return new List<string>();
        }

        /// <summary>
        /// Initializes the plugin. This method will be called by the plugin manager when all dependency plugins have
        /// been loaded.
        /// </summary>
        public void Initialize()
        {
            var pluginService = ServiceFactory.CreateByName("scripting", ContextFactory.GetContext("inter-plugin"));
            pluginService["registerGlobalObject"] = (Action<string, object>)RegisterGlobalObject;
            pluginService["addNewContextHandler"] = (Action<Action<IJSContext>>)AddNewContextHandler;

            // Register 'scripting' component.
            ComponentLayout layout = new ComponentLayout();
            layout.AddAttribute<string>(ownerScriptAttributeName);
            layout.AddAttribute<string>(serverScriptAttributeName);
            layout.AddAttribute<string> (clientScriptAttributeName);
            ComponentRegistry.Instance.DefineComponent(scriptingComponentName, pluginGUID, layout);

            EntityRegistry.Instance.OnEntityAdded += HandleOnEntityAdded;

            // Register event handlers for all entities that were created before this plugin was loaded.
            var guids = EntityRegistry.Instance.GetAllGUIDs();
            foreach (var guid in guids)
                HandleOnEntityAdded(EntityRegistry.Instance, new EntityAddedOrRemovedEventArgs(guid));

            // FIXME: This is for debugging purposes only. Remove in release.
            registeredGlobalObjects["console"] = new CLRConsole();
        }

        #endregion

        private static Logger logger = LogManager.GetCurrentClassLogger();

        private const string scriptingComponentName = "scripting";
        private const string ownerScriptAttributeName = "ownerScript";
        private const string serverScriptAttributeName = "serverScript";
        private const string clientScriptAttributeName = "clientScript";

        private void HandleOnEntityAdded(object sender, EntityAddedOrRemovedEventArgs e)
        {
            Entity newEntity = EntityRegistry.Instance.GetEntity(e.elementId);
            // FIXME: This is not very efficient. Would be nice to be triggered only when anything in "scripting" 
            // component is changed, but without the need to create one. Essentially we need an event
            // OnComponentCreated.
            newEntity.OnAttributeInComponentChanged += HandleOnAttributeInComponentChanged;
            InitEntityContext(newEntity);
        }

        private void HandleOnAttributeInComponentChanged(object sender, AttributeInComponentEventArgs e)
        {
            if (e.componentName == scriptingComponentName)
                InitEntityContext((Entity)sender);
        }


        private void InitEntityContext(Entity entity)
        {
            // Remove previous context if any.
            entityContexts.Remove(entity.Guid);

            logger.Debug("Entered initEntityContext with entity [{0}]", entity.Guid);

            string serverScript = (string)entity[scriptingComponentName][serverScriptAttributeName];
            if (serverScript == null)
                return;

            logger.Debug("Creating the context. Server script is {0}", serverScript);

            V8Engine engine;
            try {
                engine = new V8Engine();
            } catch (Exception e) {
                logger.ErrorException("Exception during context creation", e);
                return;
            }

            logger.Debug("Adding context to the list");

            entityContexts.Add(entity.Guid, engine);

            logger.Debug("Creating context wrapper");

            // This object should be used to assign event handlers, e.g. script.onNewObject = function (newObject) {...}
            var context = new V8NetContext(engine);

            logger.Debug("Creating script object");

            context.Execute("script = {}");

            logger.Debug("About to enter context scope");

            engine.WithContextScope = () => {
                logger.Debug("Configuring the context");

                // Register global objects.
                // FIXME: Potential security issue. Users can access .Type in script which allows to create any object and
                // thus run arbitrary code on the server.
                foreach (var entry in registeredGlobalObjects)
                    engine.GlobalObject.SetProperty(entry.Key, entry.Value, null, true, V8PropertyAttributes.Locked);

                logger.Debug("Calling context callbacks");

                // Invoke new context handlers.
                newContextHandlers.ForEach(handler => handler(context));

                logger.Debug("Executing serverScript");

                // Execute server script.
                engine.Execute(serverScript);
            };
        }

        private void RegisterGlobalObject(string name, object csObject)
        {
            if (registeredGlobalObjects.ContainsKey(name))
                throw new Exception("The object with " + name + " is already registered");
            registeredGlobalObjects[name] = csObject;
        }

        private void AddNewContextHandler(Action<IJSContext> handler)
        {
            newContextHandlers.Add(handler);
        }

        private Dictionary<string, object> registeredGlobalObjects = new Dictionary<string, object>();
        private List<Action<IJSContext>> newContextHandlers = new List<Action<IJSContext>>();

        private Dictionary<Guid, V8Engine> entityContexts = new Dictionary<Guid, V8Engine>();

        private readonly Guid pluginGUID = new Guid("90dd4c50-f09d-11e2-b778-0800200c9a66");
    }
}
