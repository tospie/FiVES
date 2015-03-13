using FIVES;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrokerClientPlugin
{
    public class BrokerClientPluginInitializer : IPluginInitializer
    {
        public string Name
        {
            get { return "BrokerClient"; }
        }

        public List<string> PluginDependencies
        {
            get { return new List<string>{"ClientManager", "KIARA"}; }
        }

        public List<string> ComponentDependencies
        {
            get { return new List<string>(); }
        }

        public void Initialize()
        {
            Client = BrokerClient.Instance;
        }

        public void Shutdown()
        {

        }

        public static BrokerClient Client;
    }
}
