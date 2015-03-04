using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BrokerHostPlugin
{
    public class WorldServer
    {
        public string Uri { get; set; }
        public int ConnectedClients { get; set; }
        public bool HasCapacity { get; set; }
    }
}
