﻿using BrokerClientPlugin;
using ClientManagerPlugin;
using KIARAPlugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BrokerHostPlugin
{
    public class BrokerHost
    {
        public static BrokerHost Instance
        {
            get
            {
                return instance;
            }
        }

        private BrokerHost()
        {
            RegisterClientFunctions();
        }

        private static readonly BrokerHost instance = new BrokerHost();

        private void RegisterClientFunctions()
        {
            string idlContents = File.ReadAllText("brokerhost.kiara");
            KIARAServerManager.Instance.KiaraServer.AmendIDL(idlContents);
            ClientManager.Instance.RegisterClientService("broker", false, new Dictionary<string, Delegate>{
                {"registerAsWorldServer", (Action<string>)RegisterAsWorldServer},
                {"clientConnected", (Action<string>)ClientConnectedToWorld},
                {"clientDisconnected", (Action<string>)ClientDisconnectedFromWorld},
                {"getLeastBusyServer", (Func<string>)GetLeastBusyServer}
            });
        }

        private void RegisterAsWorldServer(string uri)
        {
            Servers.Add(new WorldServer { Uri = uri, ConnectedClients = 0 });
            BrokerClient.Instance.ConnectToRemoteServer(uri);
        }

        private void ClientConnectedToWorld(string uri)
        {
            Servers.Find(server => server.Uri == uri).ConnectedClients ++;
        }

        private void ClientDisconnectedFromWorld(string uri)
        {
            Servers.Find(server => server.Uri == uri).ConnectedClients --;
        }

        private string GetLeastBusyServer()
        {
            return Servers.OrderBy(server => server.ConnectedClients).First().Uri;
        }

        private List<WorldServer> Servers = new List<WorldServer>();
    }
}
