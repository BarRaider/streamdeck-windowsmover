using BarRaider.SdTools;
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

namespace BarRaider.WindowsMover
{
    [PluginActionId("com.barraider.windowsmover")]
    public class WindowsMoverAction : WindowsActionBase
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
                    Screen = String.Empty,
                    Height = "900",
                    Width = "1500",
                    XPosition = "0",
                    YPosition = "0",
                    NoResizeWindow = true,
                    ResizeWindow = false,
                    MaximizeWindow = false,
                    MinimizeWindow = false,
                    OnlyTopmost = false,
                    ScreenFriendlyName = true,
                    TopmostWindow = false,
                    ShouldFilterLocation = false,
                    LocationFilter = String.Empty,
                    ShouldFilterTitle = false,
                    TitleFilter = String.Empty,
                    RetryAttempts = "12"
                };

                return instance;
            }

            [JsonProperty(PropertyName = "screens")]
            public List<MonitorInfo> Screens { get; set; }

            [JsonProperty(PropertyName = "topmostWindow")]
            public bool TopmostWindow { get; set; }

            [JsonProperty(PropertyName = "screen")]
            public string Screen { get; set; }

            [JsonProperty(PropertyName = "noResize")]
            public bool NoResizeWindow { get; set; }

            [JsonProperty(PropertyName = "maximizeWindow")]
            public bool MaximizeWindow { get; set; }

            [JsonProperty(PropertyName = "minimizeWindow")]
            public bool MinimizeWindow { get; set; }

            [JsonProperty(PropertyName = "resizeWindow")]
            public bool ResizeWindow { get; set; }

            [JsonProperty(PropertyName = "onlyTopmost")]
            public bool OnlyTopmost { get; set; }

            [JsonProperty(PropertyName = "height")]
            public string Height { get; set; }

            [JsonProperty(PropertyName = "width")]
            public string Width { get; set; }

            [JsonProperty(PropertyName = "xPosition")]
            public string XPosition { get; set; }

            [JsonProperty(PropertyName = "yPosition")]
            public string YPosition { get; set; }

            [JsonProperty(PropertyName = "screenFriendlyName")]
            public bool ScreenFriendlyName { get; set; }
        }

        #region Private Members

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

        private readonly System.Timers.Timer tmrRetryProcess = new System.Timers.Timer();
        private int retryAttempts = 0;

        #endregion
        public WindowsMoverAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                this.Settings = PluginSettings.CreateDefaultSettings();
            }
            else
            {
                this.Settings = payload.Settings.ToObject<PluginSettings>();
                CheckBackwardsCompability();
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
            PopulateScreens();
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
            bool screenFriendlyName = Settings.ScreenFriendlyName;
            Tools.AutoPopulateSettings(Settings, payload.Settings);
            if (screenFriendlyName != Settings.ScreenFriendlyName)
            {
                PopulateScreens();
            }


            // Make sure TopmostWindow is set, if I choose the OnlyTopmost setting
            if (Settings.OnlyTopmost && !Settings.TopmostWindow)
            {
                Settings.TopmostWindow = true;
            }

            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        #region Private Methods

        protected override Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(Settings));
        }

        private void PopulateScreens()
        {
            Settings.Screens = MonitorManager.Instance.GetAllMonitors();
            bool uniqueFriendly = MonitorManager.Instance.HasUniqueFriendlyName();
            Settings.Screens.ForEach(mon =>
            {
                mon.DisplayName = mon.DeviceName;
                if (Settings.ScreenFriendlyName)
                {
                    if (uniqueFriendly)
                    {
                        mon.DisplayName = $"{mon.FriendlyName}";
                    }
                    else
                    {
                        mon.DisplayName = $"{mon.FriendlyName} ({mon.WMIInfo.SerialNumber})";
                    }
                }
            });

            if (string.IsNullOrWhiteSpace(Settings.Screen) && Settings.Screens.Count > 0)
            {
                Settings.Screen = Settings.Screens[0].UniqueValue;
            }
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Populated {Settings.Screens.Count} screens");
        }

        private async Task MoveApplication()
        {
            if (String.IsNullOrWhiteSpace(Settings.Screen))
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"Screen not specified.");
                await Connection.ShowAlert();
                return;
            }

            if (Settings.AppSpecific && String.IsNullOrWhiteSpace(Settings.ApplicationName))
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"Application not specified.");
                await Connection.ShowAlert();
                return;
            }

            if (String.IsNullOrWhiteSpace(Settings.XPosition) || String.IsNullOrWhiteSpace(Settings.YPosition))
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"X or Y position not specified.");
                await Connection.ShowAlert();
                return;
            }


            if (!int.TryParse(Settings.XPosition, out int xPosition))
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"Invalid X position: {Settings.XPosition}");
                await Connection.ShowAlert();
                return;
            }

            if (!int.TryParse(Settings.YPosition, out int yPosition))
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"Invalid Y position: {Settings.YPosition}");
                await Connection.ShowAlert();
                return;
            }

            WindowSize windowSize = null;
            WindowResize windowResize = WindowResize.NoResize;
            if (Settings.ResizeWindow)
            {
                windowResize = WindowResize.ResizeWindow;

                if (String.IsNullOrWhiteSpace(Settings.Height) || String.IsNullOrWhiteSpace(Settings.Width))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"Height or Width position not specified.");
                    await Connection.ShowAlert();
                    return;
                }

                if (!int.TryParse(Settings.Height, out int height))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"Invalid height: {Settings.Height}");
                    await Connection.ShowAlert();
                    return;
                }

                if (!int.TryParse(Settings.Width, out int width))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"Invalid width: {Settings.Width}");
                    await Connection.ShowAlert();
                    return;
                }

                windowSize = new WindowSize(height, width);
            }
            else if (Settings.MaximizeWindow)
            {
                windowResize = WindowResize.Maximize;
            }
            else if (Settings.MinimizeWindow)
            {
                windowResize = WindowResize.Minimize;
            }
            else if (Settings.OnlyTopmost)
            {
                windowResize = WindowResize.OnlyTopmost;
            }

            Screen screen = MonitorManager.Instance.GetScreenFromUniqueValue(Settings.Screen);
            if (screen == null)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Could not find screen {Settings.Screen}");
                await Connection.ShowAlert();
                return;
            }

            var processCount = WindowPosition.MoveProcess(new MoveProcessSettings()
            {
                AppSpecific = Settings.AppSpecific,
                Name = Settings.ApplicationName,
                DestinationScreen = screen,
                Position = new System.Drawing.Point(xPosition, yPosition),
                WindowResize = windowResize,
                WindowSize = windowSize,
                MakeTopmost = Settings.TopmostWindow,
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

        private async Task FetchWindowLocation()
        {
            if (string.IsNullOrEmpty(Settings.ApplicationName))
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"FetchWindowLocation called with no application selected");
                await Connection.ShowAlert();
                return;
            }
            Logger.Instance.LogMessage(TracingLevel.INFO, $"FetchWindowLocation called");
            var rect = WindowPosition.GetWindowPostion(Settings.ApplicationName);
            if (!rect.IsEmpty)
            {
                Logger.Instance.LogMessage(TracingLevel.DEBUG, $"Rect is X: {rect.Top} Height: {rect.Bottom} Y: {rect.Left} Width: {rect.Right}");
                Settings.XPosition = rect.Left.ToString();
                Settings.YPosition = rect.Top.ToString();
                Settings.Height = rect.Height.ToString();
                Settings.Width = rect.Width.ToString();

                // Reset to first screen
                Settings.Screen = null;
                PopulateScreens();
                await SaveSettings();
            }
        }

        private async void Connection_OnSendToPlugin(object sender, SdTools.Wrappers.SDEventReceivedEventArgs<SdTools.Events.SendToPlugin> e)
        {
            var payload = e.Event.Payload;

            if (payload["property_inspector"] != null)
            {
                switch (payload["property_inspector"].ToString().ToLowerInvariant())
                {
                    case "getwindowdetails":
                        Logger.Instance.LogMessage(TracingLevel.INFO, "getWindowDetails called");
                        await FetchWindowLocation();
                        break;
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

            await MoveApplication();
        }

        private void CheckBackwardsCompability()
        {
            if (String.IsNullOrEmpty(Settings.Screen))
            {
                return;
            }

            string[] uniqueValue = Settings.Screen.Split(Constants.UNIQUE_VALUE_DELIMITER);
            if (uniqueValue.Length == 1)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"CheckBackwardsCompability found old value of {Settings.Screen}");
                var monitors = MonitorManager.Instance.GetAllMonitors();
                foreach (var monitor in monitors)
                {
                    if (monitor.DeviceName == Settings.Screen)
                    {
                        Logger.Instance.LogMessage(TracingLevel.INFO, $"CheckBackwardsCompability replacing old value of {Settings.Screen} with {monitor.UniqueValue}");
                        Settings.Screen = monitor.UniqueValue;
                        return;
                    }
                }
                Logger.Instance.LogMessage(TracingLevel.INFO, $"CheckBackwardsCompability failed for old value of {Settings.Screen}, clearing value");
                Settings.Screen = String.Empty;
                SaveSettings();
            }
        }

        #endregion
    }
}