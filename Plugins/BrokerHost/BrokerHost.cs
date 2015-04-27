// This file is part of FiVES.
//
// FiVES is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// FiVES is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with FiVES.  If not, see <http://www.gnu.org/licenses/>.
using BrokerClientPlugin;
using ClientManagerPlugin;
using KIARA;
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
                {"registerAsWorldServer", (Action<Connection, string>)RegisterAsWorldServer},
                {"clientConnected", (Action<string>)ClientConnectedToWorld},
                {"clientDisconnected", (Action<string>)ClientDisconnectedFromWorld},
                {"getLeastBusyServer", (Func<string>)GetLeastBusyServer}
            });
        }

        private void RegisterAsWorldServer(Connection connection, string uri)
        {
            Servers.Add(new WorldServer { Uri = uri, ConnectedClients = 0 });
            Console.WriteLine("[Broker Host] {0} connected as World Server ", uri);
            var newService = BrokerClient.Instance.ConnectToRemoteServer(uri);
            newService.OnConnected += (conn) => {
                BrokerClient.Instance.AssignOutgoingToIncomingConnection(conn.SessionID, connection.SessionID);
            };
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
