using ClientManagerPlugin;
using FIVES;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrowserPerformanceTestsPlugin
{
    public class BrowserPerformancePluginInitializer : IPluginInitializer
    {
        public string Name
        {
            get { return "BrowserPerformanceTests"; }
        }

        public List<string> PluginDependencies
        {
            get { return new List<string> {"KIARA", "ClientManager" }; }
        }

        public List<string> ComponentDependencies
        {
            get { return new List<string>(); }
        }

        public void Initialize()
        {
            RegisterComponent();
            AmendKiaraIDL();
            RegisterClientService();
        }

        public void Shutdown()
        {         
        }

        private void RegisterComponent()
        {
            ComponentDefinition performanceTestComponent = new ComponentDefinition("performanceTest");
            performanceTestComponent.AddAttribute<double>("roundtripTimestamp");
            ComponentRegistry.Instance.Register(performanceTestComponent);
        }

        private void AmendKiaraIDL()
        {
            string idlContent = File.ReadAllText("browserPerformance.kiara");
            KIARAPlugin.KIARAServerManager.Instance.KiaraServer.AmendIDL(idlContent);
        }

        private void RegisterClientService()
        {
            ClientManager.Instance.RegisterClientService("browserPerformance", false, new Dictionary<string, Delegate>
            {
                {"invokeRoundtrip", (Action<string, double>)InvokeRoundtrip}
            });
        }

        private void InvokeRoundtrip(string testEntityGuid, double timeStamp)
        {
            var e = World.Instance.FindEntity(testEntityGuid);
            e["performanceTest"]["roundtripTimestamp"].Suggest(timeStamp);
        }
    }
}
