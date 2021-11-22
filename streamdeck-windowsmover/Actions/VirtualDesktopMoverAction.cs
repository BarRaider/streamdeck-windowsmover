using BarRaider.SdTools;
using BarRaider.WindowsMover.Backend;
using BarRaider.WindowsMover.Internal;
using BarRaider.WindowsMover.MonitorWrapper;
using BarRaider.WindowsMover.Wrappers;
using BarRaiderVirtualDesktop.VirtualDesktop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarRaider.WindowsMover
{
    [PluginActionId("com.barraider.vdmover")]
    public class VirtualDesktopMoverAction : WindowsActionBase
    {
        private class PluginSettings : PluginSettingsBase
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings
                {
                    AppSpecific = true,
                    AppCurrent = false,
                    ApplicationName = String.Empty,
                    ShouldFilterLocation = false,
                    LocationFilter = String.Empty,
                    ShouldFilterTitle = false,
                    TitleFilter = String.Empty,
                    RetryAttempts = "12",
                    DesktopName = String.Empty,
                    Desktops = null
                };

                return instance;
            }
            [JsonProperty(PropertyName = "desktopName")]
            public string DesktopName { get; set; }

            [JsonProperty(PropertyName = "desktops")]
            public List<VirtualDesktopInfo> Desktops { get; set; }
        }

        private PluginSettings Settings
        {
            get
            {
                var result = settings as PluginSettings;
                if (result == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, "Cannot convert PluginSettingsBase to PluginSettings");
                }
                return result;
            }
            set
            {
                settings = value;
            }
        }

        #region Private Members

        private readonly System.Timers.Timer tmrRetryProcess = new System.Timers.Timer();
        private int retryAttempts = 0;

        #endregion
        public VirtualDesktopMoverAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                this.Settings = PluginSettings.CreateDefaultSettings();
            }
            else
            {
                this.Settings = payload.Settings.ToObject<PluginSettings>();
            }
            Connection.OnSendToPlugin += Connection_OnSendToPlugin;
            tmrRetryProcess.Interval = 5000;
            tmrRetryProcess.Elapsed += TmrRetryProcess_Elapsed;

            // Used for backward compatibility 
            if (!Settings.AppSpecific && !Settings.AppCurrent)
            {
                Settings.AppSpecific = true;
            }

            PopulateApplications();
            InitializeSettings();
            SaveSettings();
        }

        public override void Dispose()
        {
            tmrRetryProcess.Stop();
            Connection.OnSendToPlugin -= Connection_OnSendToPlugin;
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Destructor called");
        }

        public async override void KeyPressed(KeyPayload payload)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "Key Pressed");

            await MoveApplication();
        }

        public override void KeyReleased(KeyPayload payload) { }

        public async override void OnTick()
        {
            if (Settings.AppSpecific && !String.IsNullOrEmpty(Settings.ApplicationName))
            {
                await Connection.SetTitleAsync(Settings.ApplicationName);
            }
            else
            {
                await Connection.SetTitleAsync(null);
            }
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(Settings, payload.Settings);
            InitializeSettings();
            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        #region Private Methods

        protected override Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(Settings));
        }

       
        private async Task MoveApplication()
        {
            if (String.IsNullOrWhiteSpace(Settings.DesktopName))
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"MoveApplication called but DesktopName is null!");
                await Connection.ShowAlert();
                return;
            }

            if (Settings.AppSpecific && String.IsNullOrWhiteSpace(Settings.ApplicationName))
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"MoveApplication called but Application not specified.");
                await Connection.ShowAlert();
                return;
            }

            var processCount = VirtualDesktopProcessMover.MoveProcess(new VirtualDesktopProcessMoverSettings()
            {
                AppSpecific = Settings.AppSpecific,
                Name = Settings.ApplicationName,
                VirtualDesktopName = Settings.DesktopName,
                LocationFilter = Settings.ShouldFilterLocation ? Settings.LocationFilter : null,
                TitleFilter = Settings.ShouldFilterTitle ? Settings.TitleFilter : null
            });

            if (processCount > 0)
            {
                tmrRetryProcess.Stop();
            }
            else if (processCount == 0 && !tmrRetryProcess.Enabled)
            {
                if (!Int32.TryParse(Settings.RetryAttempts, out retryAttempts))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"Invalid RetryAttempts: {Settings.RetryAttempts}");
                    return;
                }
                tmrRetryProcess.Start();
            }
        }

        private async void Connection_OnSendToPlugin(object sender, SdTools.Wrappers.SDEventReceivedEventArgs<SdTools.Events.SendToPlugin> e)
        {
            var payload = e.Event.Payload;

            if (payload["property_inspector"] != null)
            {
                switch (payload["property_inspector"].ToString().ToLowerInvariant())
                {
                    case "reloadapps":
                        Logger.Instance.LogMessage(TracingLevel.INFO, "reloadApps called");
                        PopulateApplications();
                        await SaveSettings();
                        break;
                    case "refreshdesktops":
                        Logger.Instance.LogMessage(TracingLevel.INFO, $"refreshDesktops called");
                        FetchAllVirtualDesktops();
                        break;
                }
            }
        }

        private async void TmrRetryProcess_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            retryAttempts--;

            if (retryAttempts <= 0)
            {
                tmrRetryProcess.Stop();
                return;
            }

            await MoveApplication();
        }

        private void InitializeSettings()
        {
            FetchAllVirtualDesktops();
        }

        private void FetchAllVirtualDesktops()
        {
            Settings.Desktops = new List<VirtualDesktopInfo>();
            try
            {
                for (int currDesktop = 0; currDesktop < VirtualDesktopManager.Instance.Count(); currDesktop++)
                {
                    Settings.Desktops.Add(new VirtualDesktopInfo() { Name = VirtualDesktopManager.Instance.DesktopNameFromIndex(currDesktop) });
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"FetchAllVirtualDesktops Exception: {ex}");
            }
            SaveSettings();
        }

        #endregion
    }
}