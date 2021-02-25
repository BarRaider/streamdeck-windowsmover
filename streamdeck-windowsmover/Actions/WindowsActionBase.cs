using BarRaider.SdTools;
using BarRaider.WindowsMover.Backend;
using BarRaider.WindowsMover.Internal;
using BarRaider.WindowsMover.MonitorWrapper;
using BarRaider.WindowsMover.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using VirtualDesktop;

namespace BarRaider.WindowsMover
{
    public class WindowsActionBase : PluginBase
    {
        protected class PluginSettingsBase
        {
            [JsonProperty(PropertyName = "applicationName")]
            public string ApplicationName { get; set; }

            [JsonProperty(PropertyName = "applications")]
            public List<ProcessInfo> Applications { get; set; }

            [JsonProperty(PropertyName = "filterLocation")]
            public bool ShouldFilterLocation { get; set; }

            [JsonProperty(PropertyName = "locationFilter")]
            public string LocationFilter { get; set; }

            [JsonProperty(PropertyName = "filterTitle")]
            public bool ShouldFilterTitle { get; set; }

            [JsonProperty(PropertyName = "titleFilter")]
            public string TitleFilter { get; set; }

            [JsonProperty(PropertyName = "retryAttempts")]
            public string RetryAttempts { get; set; }

            [JsonProperty(PropertyName = "appSpecific")]
            public bool AppSpecific { get; set; }

            [JsonProperty(PropertyName = "appCurrent")]
            public bool AppCurrent { get; set; }

        }

        #region Private Members

        protected  PluginSettingsBase settings;

        #endregion
        public WindowsActionBase(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
        }

        public override void Dispose()
        {
        }

        public override void KeyPressed(KeyPayload payload) { }


        public override void KeyReleased(KeyPayload payload) { }

        public async override void OnTick()
        {
            if (settings.AppSpecific && !String.IsNullOrEmpty(settings.ApplicationName))
            {
                await Connection.SetTitleAsync(settings.ApplicationName);
            }
            else
            {
                await Connection.SetTitleAsync(null);
            }
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        #region Private Methods


        protected virtual Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        protected void PopulateApplications()
        {
            settings.Applications = System.Diagnostics.Process.GetProcesses().Select(p => new ProcessInfo(p.ProcessName)).GroupBy(p => p.Name).Select(p => p.First()).OrderBy(p => p.Name).ToList();
            if (string.IsNullOrEmpty(settings.ApplicationName) && settings.Applications.Count > 0)
            {
                settings.ApplicationName = settings.Applications[0].Name;
            }
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Populated {settings.Applications.Count} applications");
        }

        #endregion
    }
}