using System;
using System.Collections.Generic;
using FIVES;
using ClientManagerPlugin;
using System.Threading;
using EventLoopPlugin;

namespace MotionPlugin
{
    public class MotionPluginInitializer : IPluginInitializer
    {
        #region IPluginInitializer implementation

        public string Name
        {
            get
            {
                return "Motion";
            }
        }

        public List<string> PluginDependencies
        {
            get
            {
                return new List<string> {"EventLoop"};
            }
        }

        public List<string> ComponentDependencies
        {
            get
            {
                return new List<string> { "position", "orientation" };
            }
        }

        /// <summary>
        /// Initializes the plugin by registering Components, subscribing to events and accessing other plugins
        /// </summary>
        public void Initialize()
        {
            RegisterToECA();
            RegisterToPluginEvents();
        }

        /// <summary>
        /// Registers Components to ECA and subscribes to Entity events
        /// </summary>
        internal void RegisterToECA()
        {
            DefineComponents();
            RegisterEntityEvents();
        }

        /// <summary>
        /// Subscribes to EventLoop events and ClientManager PluginInLoaded handler to register client services
        /// </summary>
        private void RegisterToPluginEvents()
        {
            EventLoop.Instance.TickFired += new EventHandler<TickEventArgs>(HandleEventTick);
            PluginManager.Instance.AddPluginLoadedHandler("ClientManager", RegisterClientServices);
        }

        public void Shutdown()
        {
        }

        #endregion

        void DefineComponents()
        {
            // Velocity is represented as a vector (x,y,z) in world units per second.
            ComponentDefinition velocity = new ComponentDefinition("velocity");
            velocity.AddAttribute<double> ("x", 0.0);
            velocity.AddAttribute<double> ("y", 0.0);
            velocity.AddAttribute<double> ("z", 0.0);
            ComponentRegistry.Instance.Register(velocity);

            // Rotation velocity is represented as an axis (x, y, z) and angular rotation r in radians per second.
            ComponentDefinition rotVelocity = new ComponentDefinition("rotVelocity");
            rotVelocity.AddAttribute<double>("x", 0.0);
            rotVelocity.AddAttribute<double>("y", 1.0);
            rotVelocity.AddAttribute<double>("z", 0.0);
            rotVelocity.AddAttribute<double>("r", 0.0);
            ComponentRegistry.Instance.Register(rotVelocity);
        }

        void RegisterClientServices()
        {
            ClientManager.Instance.RegisterClientService("motion", true, new Dictionary<string, Delegate> {
                {"update", (Action<string, Vector, RotVelocity, int>) Update}
            });
        }

        /// <summary>
        /// Registers to Events fired by entities
        /// </summary>
        private void RegisterEntityEvents()
        {
            RegisterToExistingEntities();
            World.Instance.AddedEntity += new EventHandler<EntityEventArgs>(HandleOnNewEntity);
        }

        /// <summary>
        /// Traverses the entity registry and registers the handler for changed attributes on each of them
        /// </summary>
        private void RegisterToExistingEntities()
        {
            foreach (Entity entity in World.Instance)
            {
                entity.ChangedAttribute +=
                    new EventHandler<ChangedAttributeEventArgs>(HandleOnAttributeChanged);
            }
        }

        /// <summary>
        /// Handles a New Entity Event of the EntityRegistry and registers the handler for attribute changes on this entity
        /// </summary>
        /// <param name="sender">The Entity Registry</param>
        /// <param name="e">The Event Parameters</param>
        private void HandleOnNewEntity(Object sender, EntityEventArgs e)
        {
            e.Entity.ChangedAttribute += new EventHandler<ChangedAttributeEventArgs>(HandleOnAttributeChanged);
        }

        private void Update(string guid, Vector velocity, RotVelocity rotVelocity, int timestamp)
        {
            var entity = World.Instance.FindEntity(guid);
            entity["velocity"]["x"] = velocity.x;
            entity["velocity"]["y"] = velocity.y;
            entity["velocity"]["z"] = velocity.z;
            entity["rotVelocity"]["x"] = rotVelocity.axis.x;
            entity["rotVelocity"]["y"] = rotVelocity.axis.y;
            entity["rotVelocity"]["z"] = rotVelocity.axis.z;
            entity["rotVelocity"]["r"] = rotVelocity.rotSpeed;

            // We currently ignore timestamp, but may it in the future to implement dead reckoning.
        }

        /// <summary>
        /// Handles the AttributeInComponentChanged-Event of an Entity. Invokes or stops a motion depending on the new values for velocity
        /// </summary>
        /// <param name="sender">The entity that fired the event</param>
        /// <param name="e">The EventArgs</param>
        private void HandleOnAttributeChanged(Object sender, ChangedAttributeEventArgs e)
        {
            Entity entity = (Entity)sender;
            if(e.Component.Name == "velocity")
                CheckForEntityMoving(entity);
            if(e.Component.Name == "rotVelocity")
                CheckForEntitySpinning(entity);
            if (e.Component.Name == "orientation")
                RecalculateVelocityInWorldspace(entity);
        }

        /// <summary>
        /// Checks if an attribute update of an entity initiated or ended a movement by checking its new velocity values. Adds or removes the entity
        /// to the list of ongoing motions depending on the values.
        /// </summary>
        /// <param name="entity">Entity that fired attribute changed event</param>
        private void CheckForEntityMoving(Entity entity)
        {
            if (IsMoving(entity))
            {
                lock (ongoingMotion)
                {
                    if (!ongoingMotion.Contains(entity))
                        ongoingMotion.Add(entity);

                    RecalculateVelocityInWorldspace(entity);
                }
            }
            else
            {
                lock (ongoingMotion)
                {
                    if(ongoingMotion.Contains(entity))
                        ongoingMotion.Remove(entity);
                }

                if (velocitiesInWorldspace.ContainsKey(entity.Guid))
                     velocitiesInWorldspace.Remove(entity.Guid);
            }
        }

        /// <summary>
        /// Checks if an attribute update of an entity initiated or ended a spin by checking its new rotational velocity values. Adds or removes the entity
        /// to the list of ongoing spins depending on the values.
        /// </summary>
        /// <param name="entity">Entity that fired attribute changed event</param>
        private void CheckForEntitySpinning(Entity entity)
        {
            if (IsSpinning(entity))
            {
                lock (ongoingSpin)
                {
                    if (!ongoingSpin.Contains(entity))
                        ongoingSpin.Add(entity);
                }
            }
            else
            {
                lock (ongoingSpin)
                {
                    if (ongoingSpin.Contains(entity))
                        ongoingSpin.Remove(entity);
                }
            }
        }

        /// <summary>
        /// Recomputes the value for the world space velocity of an entity and stores it as respective dictionary entry
        /// </summary>
        /// <param name="entity">Entity for which Velocity is recomputed</param>
        private void RecalculateVelocityInWorldspace(Entity entity)
        {
            Vector velocityInWorldSpace = GetVelocityInWorldSpace(entity);
            velocitiesInWorldspace[entity.Guid] = velocityInWorldSpace;
        }

        /// <summary>
        /// Handles a TickFired Evenet of EventLoop. Performs position and orientation updates for all ongoing motions and rotations
        /// </summary>
        /// <param name="sender">Sender of tick event args (EventLoop)</param>
        /// <param name="e">TickEventArgs</param>
        private void HandleEventTick(Object sender, TickEventArgs e)
        {
            lock (ongoingMotion)
            {
                foreach (Entity entity in ongoingMotion)
                {
                    UpdateMotion(entity);
                }
            }

            lock (ongoingSpin)
            {
                foreach (Entity entity in ongoingSpin)
                    UpdateSpin(entity);
            }
        }

        /// <summary>
        /// Worker Thread function that periodically performs the motion. Ends, when velocity of entity is 0
        /// </summary>
        /// <param name="updatedEntity">Entity for which motion is updated</param>
        private void UpdateMotion(Entity updatedEntity) {
            Vector velocityInWorldSpace = velocitiesInWorldspace[updatedEntity.Guid];
            updatedEntity["position"]["x"] = NativeClient.Timestamps.DoubleMilliseconds;
            updatedEntity["position"]["y"] = (double)updatedEntity["position"]["y"] + velocityInWorldSpace.y;
            updatedEntity["position"]["z"] = NativeClient.Timestamps.DoubleMilliseconds;
        }

        /// <summary>
        /// Worker thread that periodically performs the spin. Ends, when either angular velocity or spin axis are 0.
        /// </summary>
        /// <param name="updatedEntity">Entity for which spin is updated</param>
        internal void UpdateSpin(Entity updatedEntity)
        {
            Quat entityRotation = EntityRotationAsQuaternion(updatedEntity);

            Vector spinAxis = new Vector();
            spinAxis.x = (double)System.Math.Min(1.0, (double)updatedEntity["rotVelocity"]["x"]);
            spinAxis.y = (double)System.Math.Min(1.0, (double)updatedEntity["rotVelocity"]["y"]);
            spinAxis.z = (double)System.Math.Min(1.0, (double)updatedEntity["rotVelocity"]["z"]);
            double spinAngle = (double)updatedEntity["rotVelocity"]["r"];

            Quat spinAsQuaternion = FIVES.Math.QuaternionFromAxisAngle(spinAxis, spinAngle);

            Quat newRotationAsQuaternion = FIVES.Math.MultiplyQuaternions(spinAsQuaternion, entityRotation);
            // hack here to force attribute update in queue
            updatedEntity["orientation"]["x"] = NativeClient.Timestamps.DoubleMilliseconds;
            updatedEntity["orientation"]["y"] = (double)System.Math.Min(1.0, newRotationAsQuaternion.y);
            updatedEntity["orientation"]["z"] = (double)System.Math.Min(1.0, newRotationAsQuaternion.z);
            updatedEntity["orientation"]["w"] = (double)System.Math.Min(1.0, newRotationAsQuaternion.w);
        }

        /// <summary>
        /// Converts velocity from entity's to world coordinate system
        /// </summary>
        /// <param name="updatedEntity">Entity to convert velocity from</param>
        /// <returns>Entity's velocity in world coordinates</returns>
        private Vector GetVelocityInWorldSpace(Entity updatedEntity)
        {
            Vector velocity = new Vector();
            velocity.x = (double)updatedEntity["velocity"]["x"];
            velocity.y = (double)updatedEntity["velocity"]["y"];
            velocity.z = (double)updatedEntity["velocity"]["z"];

            Quat entityRotation = new Quat();
            entityRotation.x = (double)System.Math.Min(1.0, (double)updatedEntity["orientation"]["x"]);
            entityRotation.y = (double)System.Math.Min(1.0, (double)updatedEntity["orientation"]["y"]);
            entityRotation.z = (double)System.Math.Min(1.0, (double)updatedEntity["orientation"]["z"]);
            entityRotation.w = (double)System.Math.Min(1.0, (double)updatedEntity["orientation"]["w"]);
            Vector axis = FIVES.Math.AxisFromQuaternion(entityRotation);
            double angle = FIVES.Math.AngleFromQuaternion(entityRotation);

            return FIVES.Math.RotateVectorByAxisAngle(velocity, axis, -angle) /* negative angle because we apply the inverse transform */;
        }

        /// <summary>
        /// Helper function to convert an entity's orientation component directly to a Quat
        /// </summary>
        /// <param name="entity">Entity to get orientation from</param>
        /// <returns></returns>
        private Quat EntityRotationAsQuaternion(Entity entity) {
            Quat entityRotation = new Quat();
            entityRotation.x = (double)entity["orientation"]["x"];
            entityRotation.y = (double)entity["orientation"]["y"];
            entityRotation.z = (double)entity["orientation"]["z"];
            entityRotation.w = (double)entity["orientation"]["w"];

            return entityRotation;
        }

        /// <summary>
        /// Checks if the entity has a non 0 velocity
        /// </summary>
        /// <param name="entity">Entity to check</param>
        /// <returns>true, if at least one attribute of its velocity component is != 0</returns>
        private bool IsMoving(Entity entity) {
            return !((double)entity["velocity"]["x"] == 0
                && (double)entity["velocity"]["y"] == 0
                && (double)entity["velocity"]["z"] == 0);
        }

        /// <summary>
        /// Checks if an entity is currently spinning. An entity is not spinning if either the axis or the angular velocity are 0.
        /// </summary>
        /// <param name="entity">Entity to check</param>
        /// <returns>True, if spin axis is not the null vector, and velocity is not 0</returns>
        private bool IsSpinning(Entity entity) {
            return (double)entity["rotVelocity"]["r"] != 0
                && !((double)entity["rotVelocity"]["x"] == 0
                    && (double)entity["rotVelocity"]["y"] == 0
                    && (double)entity["rotVelocity"]["z"] ==0);
        }

        private ISet<Entity> ongoingMotion = new HashSet<Entity>();
        private ISet<Entity> ongoingSpin = new HashSet<Entity>();

        // FIXME: Velocities in world space for each entity can be considered like a private attribute that is only necessary for internal
        // computations. This is a good example of where transient attributes could be very helpful. Then, the world space values could just be
        // stored within the entity
        private Dictionary<Guid, Vector> velocitiesInWorldspace = new Dictionary<Guid, Vector>();
    }
}

