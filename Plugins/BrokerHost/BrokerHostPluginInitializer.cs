using FIVES;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrokerHostPlugin
{
    public class BrokerHostPluginInitializer : IPluginInitializer
    {
        public string Name
        {
            get { return "BrokerHost"; }
        }

        public List<string> PluginDependencies
        {
            get { return new List<string>(); }
        }

        public List<string> ComponentDependencies
        {
            get { return new List<string>(); }
        }

        public void Initialize()
        {
            Broker = BrokerHost.Instance;
        }

        public void Shutdown()
        {
            throw new NotImplementedException();
        }

        public static BrokerHost Broker;
    }
}
