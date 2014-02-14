﻿using FIVES;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using EventLoopPlugin;
using KeyframeAnimationPlugin;

namespace BotEntitiesPlugin
{
    public class BotEntitiesPluginInitializer : IPluginInitializer
    {
        public string Name
        {
            get { return "BotEntities"; }
        }

        public List<string> PluginDependencies
        {
            get { return new List<string> { "EventLoop"}; }
        }

        public List<string> ComponentDependencies
        {
            get { return new List<string> { "meshResource", "velocity", "rotVelocity" };  }
        }

        public void Initialize()
        {
           //RetrieveConfigurationValues();
           CreateBotEntities();
           EventLoop.Instance.TickFired += new EventHandler<TickEventArgs>(HandleEventTick);
        }

        public void Shutdown()
        {
            Console.WriteLine("Wheeeeeeeeeeeeeeeee ....");
        }

        private void RetrieveConfigurationValues()
        {
            string configPath = this.GetType().Assembly.Location;
            Configuration config = ConfigurationManager.OpenExeConfiguration(configPath);

            int.TryParse(ConfigurationManager.AppSettings["numBots"], out numBots);
            botMesh = ConfigurationManager.AppSettings["botMesh"];
            double.TryParse(ConfigurationManager.AppSettings["walkSpeed"], out botWalkSpeed);
            double.TryParse(ConfigurationManager.AppSettings["rotateSpeed"], out botRotateSpeed);
            int.TryParse(ConfigurationManager.AppSettings["updateInterval"], out botUpdateInterval);
        }

        private void CreateBotEntities()
        {
            for (var i = 0; i < numBots; i++)
            {
                Entity botEntity = new Entity();
                botEntity["position"]["x"] = 0.0;
                botEntity["position"]["y"] = 0.0;
                botEntity["position"]["z"] = 0.0;

                botEntity["rotVelocity"]["x"] = 0.0;
                botEntity["rotVelocity"]["y"] = 1.0;
                botEntity["rotVelocity"]["z"] = 0.0;
                botEntity["rotVelocity"]["r"] = 0.0;

                botEntity["meshResource"]["uri"] = botMesh;
                FIVES.World.Instance.Add(botEntity);
                bots.Add(botEntity);
            }
        }

        private void HandleEventTick(Object sender, TickEventArgs e)
        {
            millisecondsSinceLastTick += EventLoop.Instance.TickInterval;
            if (millisecondsSinceLastTick > botUpdateInterval)
            {
                foreach (Entity botEntity in bots)
                {
                    int moveRoll = random.Next(10);
                    if (moveRoll > 8)
                        ChangeMoveSpeed(botEntity);
                    if (moveRoll < 2)
                        ChangeRotateSpeed(botEntity);
                }
            }
        }

        private void ChangeMoveSpeed(Entity botEntity)
        {
            if ((double)botEntity["velocity"]["x"] == botWalkSpeed)
            {
                botEntity["velocity"]["x"] = 0.0;
                StopWalkAnimation(botEntity);
            }
            else
            {
                botEntity["velocity"]["x"] = botWalkSpeed;
                PlayWalkAnimation(botEntity);
            }
        }

        private void ChangeRotateSpeed(Entity botEntity)
        {
            int rotateRoll = random.Next(10);
            if (rotateRoll == 8)
            {
                botEntity["rotVelocity"]["r"] = botRotateSpeed;
                PlayWalkAnimation(botEntity);
            }

            else if (rotateRoll == 9)
            {
                botEntity["rotVelocity"]["r"] = -botRotateSpeed;
                PlayWalkAnimation(botEntity);
            }
            else
            {
                botEntity["rotVelocity"]["r"] = 0.0;
                StopWalkAnimation(botEntity);
            }
        }

        private void PlayWalkAnimation(Entity botEntity)
        {
            if (!KeyframeAnimationManager.Instance.IsPlaying(botEntity.Guid, "walk"))
            {
                KeyframeAnimation walkAnimation = new KeyframeAnimation("walk", 0f, 7.1f, -1, 1);
                KeyframeAnimationManager.Instance.StartAnimation(botEntity.Guid, walkAnimation);
            }
        }

        private void StopWalkAnimation(Entity botEntity)
        {
            if ((double)botEntity["velocity"]["x"] == 0.0 && (double)botEntity["rotVelocity"]["r"] == 0.0)
                KeyframeAnimationManager.Instance.StopAnimation(botEntity.Guid, "walk");
        }

        private int numBots = 40;
        private HashSet<Entity> bots = new HashSet<Entity>();
        private string botMesh = "/models/natalieFives/xml3d/natalie.xml";
        private double botWalkSpeed = 0.05;
        private double botRotateSpeed = 0.05;
        private int millisecondsSinceLastTick = 0;
        private int botUpdateInterval = 5000;
        private Random random = new Random();
    }
}
