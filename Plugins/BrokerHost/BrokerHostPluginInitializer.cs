﻿using FIVES;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrokerHost
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
            throw new NotImplementedException();
        }

        public void Shutdown()
        {
            throw new NotImplementedException();
        }
    }
}
