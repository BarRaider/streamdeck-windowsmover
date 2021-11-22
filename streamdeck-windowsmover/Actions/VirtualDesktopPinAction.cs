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
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarRaider.WindowsMover
{
    [PluginActionId("com.barraider.vdpin")]
    public class VirtualDesktopPinAction : WindowsActionBase
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
                    ModePin = true,
                    ModeUnpin = false,
                    ModePinToggle = false
                };

                return instance;
            }
            [JsonProperty(PropertyName = "modePin")]
            public bool ModePin { get; set; }

            [JsonProperty(PropertyName = "modeUnpin")]
            public bool ModeUnpin { get; set; }

            [JsonProperty(PropertyName = "modePinToggle")]
            public bool ModePinToggle { get; set; }
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
        public VirtualDesktopPinAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
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

            await PinApplication();
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


        private async Task PinApplication()
        {
            if (Settings.AppSpecific && String.IsNullOrWhiteSpace(Settings.ApplicationName))
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"MoveApplication called but Application not specified.");
                await Connection.ShowAlert();
                return;
            }

            var processCount = HandleApplicationPin();

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

            await PinApplication();
        }

        private void InitializeSettings()
        {
            if (!Settings.ModePin && !Settings.ModeUnpin && !Settings.ModePinToggle)
            {
                Settings.ModePin = true;
                SaveSettings();
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private int HandleApplicationPin()
        {
            try
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Changing Pin to mode: [Pin {Settings.ModePin} UnPin: {Settings.ModeUnpin} Toggle: {Settings.ModePinToggle}] for: AppSpecific: {Settings.AppSpecific} ProcessName: {Settings.ApplicationName}  LocationFilter: {Settings.LocationFilter} TitleFilter: {Settings.TitleFilter}");
                if (!settings.AppSpecific) // Move the current window
                {
                    HandleWindowPin(GetForegroundWindow());
                    return 1;
                }
                else
                {
                    int totalProcesses = 0;
                    int movedProcesses = 0;
                    foreach (var process in System.Diagnostics.Process.GetProcessesByName(settings.ApplicationName))
                    {
                        try
                        {
                            totalProcesses++;
                            IntPtr h1 = process.MainWindowHandle;
                            if (h1.ToInt32() == 0)
                            {
                                continue;
                            }
                            Logger.Instance.LogMessage(TracingLevel.INFO, $"Found {settings.ApplicationName} with handle {h1}");

                            if (!String.IsNullOrEmpty(settings.LocationFilter) && !process.MainModule.FileName.ToLowerInvariant().Contains(settings.LocationFilter.ToLowerInvariant()))
                            {
                                Logger.Instance.LogMessage(TracingLevel.INFO, $"Skipped {settings.ApplicationName} with handle {h1} as the file location was different from \"{settings.LocationFilter}\": {process.MainModule.FileName}");
                                continue;
                            }

                            if (!String.IsNullOrEmpty(settings.TitleFilter) && !process.MainWindowTitle.Contains(settings.TitleFilter))
                            {
                                Logger.Instance.LogMessage(TracingLevel.INFO, $"Skipped {settings.ApplicationName} with handle {h1} as the window title was different from \"{settings.TitleFilter}\": {process.MainWindowTitle}");
                                continue;
                            }
                            movedProcesses++;
                            HandleWindowPin(h1);
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error setting Virtual Desktop for process {settings.ApplicationName} {ex}");
                        }
                    }
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"Iterated through {totalProcesses} processes, moved {movedProcesses}");

                    return movedProcesses;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"MoveProcess Exception {ex}");
            }
            return 0;
        }

        private void HandleWindowPin(IntPtr hwnd)
        {
            try
            {
                bool shouldPin = Settings.ModePin;
                if (Settings.ModePinToggle)
                {
                    shouldPin = !VirtualDesktopManager.Instance.IsApplicationPinned(hwnd);
                }

                if (shouldPin)
                {
                    VirtualDesktopManager.Instance.PinApplication(hwnd);
                }
                else
                {
                    VirtualDesktopManager.Instance.UnpinApplication(hwnd);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"HandleWindowPin Exception for hwnd {hwnd}: {ex}");
            }
        }

        #endregion
    }
}