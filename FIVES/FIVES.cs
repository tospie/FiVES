using System;
using System.Configuration;
using NLog;
using System.IO;

namespace FIVES
{
    public class MainClass
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        static int Main(string[] args)
        {
            ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            try {
                string pluginDir = ConfigurationManager.AppSettings["PluginsDir"].ToString();
                if (Directory.Exists(pluginDir))
                    PluginManager.Instance.loadPluginsFrom(pluginDir);
            } catch (NullReferenceException) {
                logger.Error("Plugins dir is not specified.");
            } catch (ConfigurationErrorsException) {
                logger.Error("Configuration is missing or corrupt.");
            }

            // Add 5 entities.
            var random = new Random();
            for (int i = 0; i < 5; i++) {
                dynamic entity = new Entity();
                entity.position.x = (float)(random.NextDouble() * 100 - 50);
                entity.position.y = (float)(random.NextDouble() * 100 - 50);
                entity.position.z = (float)(random.NextDouble() * 100 - 50);

                entity.scale.x = 1.0f;
                entity.scale.y = 1.0f;
                entity.scale.z = 1.0f;

                entity.meshResource.uri = "my://path.to/resource";

                EntityRegistry.Instance.addEntity(entity);
            }

            // Wait for any key to be pressed.
            Console.ReadKey();

            return 0;
        }
    }
}

