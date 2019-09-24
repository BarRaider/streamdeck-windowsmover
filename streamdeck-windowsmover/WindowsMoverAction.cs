using BarRaider.SdTools;
using BarRaider.WindowsMover.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarRaider.WindowsMover
{
    [PluginActionId("com.barraider.windowsmover")]
    public class WindowsMoverAction : PluginBase
    {
        private class PluginSettings
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

            [JsonProperty(PropertyName = "applicationName")]
            public string ApplicationName { get; set; }

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

            [JsonProperty(PropertyName = "applications")]
            public List<ProcessInfo> Applications { get; set; }

            [JsonProperty(PropertyName = "screens")]
            public List<ScreenInfo> Screens { get; set; }

            [JsonProperty(PropertyName = "topmostWindow")] 
            public bool TopmostWindow { get; set; }

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

        private readonly PluginSettings settings;
        private readonly System.Timers.Timer tmrRetryProcess = new System.Timers.Timer();
        private int retryAttempts = 0;

        #endregion
        public WindowsMoverAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            if (payload.Settings == null || payload.Settings.Count == 0)
            {
                this.settings = PluginSettings.CreateDefaultSettings();
            }
            else
            {
                this.settings = payload.Settings.ToObject<PluginSettings>();
            }
            connection.StreamDeckConnection.OnSendToPlugin += StreamDeckConnection_OnSendToPlugin;
            tmrRetryProcess.Interval = 5000;
            tmrRetryProcess.Elapsed += TmrRetryProcess_Elapsed;

            // Used for backward compatibility 
            if (!settings.AppSpecific && !settings.AppCurrent)
            {
                settings.AppSpecific = true;
            }

            PopulateApplications();
            PopulateScreens();
            SaveSettings();
        }

        public override void Dispose()
        {
            Connection.StreamDeckConnection.OnSendToPlugin -= StreamDeckConnection_OnSendToPlugin;
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
            bool screenFriendlyName = settings.ScreenFriendlyName;
            Tools.AutoPopulateSettings(settings, payload.Settings);
            if (screenFriendlyName != settings.ScreenFriendlyName)
            {
                PopulateScreens();
            }


            // Make sure TopmostWindow is set, if I choose the OnlyTopmost setting
            if (settings.OnlyTopmost && !settings.TopmostWindow)
            {
                settings.TopmostWindow = true;
            }

            SaveSettings();
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload) { }

        #region Private Methods


        private Task SaveSettings()
        {
            return Connection.SetSettingsAsync(JObject.FromObject(settings));
        }

        private void PopulateApplications()
        {
            settings.Applications = System.Diagnostics.Process.GetProcesses().Select(p => new ProcessInfo(p.ProcessName)).GroupBy(p => p.Name).Select(p => p.First()).OrderBy(p => p.Name).ToList();
            if (string.IsNullOrEmpty(settings.ApplicationName) && settings.Applications.Count > 0)
            {
                settings.ApplicationName = settings.Applications[0].Name;
            }
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Populated {settings.Applications.Count} applications");
        }

        private void PopulateScreens()
        {
            settings.Screens = Screen.AllScreens.Select(s =>
            {
                string friendlyName = s.DeviceName;
                if (settings.ScreenFriendlyName)
                {
                    try
                    {
                        string friendlyNameStr = s.DeviceFriendlyName();
                        if (!String.IsNullOrEmpty(friendlyNameStr))
                        {
                            friendlyName = friendlyNameStr;
                        }
                    }
                    catch { }
                }
                return new ScreenInfo(s.DeviceName, friendlyName);
            }).ToList();

            if (string.IsNullOrWhiteSpace(settings.Screen) && settings.Screens.Count > 0)
            {
                settings.Screen = settings.Screens[0].DeviceName;
            }
            Logger.Instance.LogMessage(TracingLevel.INFO, $"Populated {settings.Screens.Count} screens");
        }

        private async Task MoveApplication()
        {
            if (String.IsNullOrWhiteSpace(settings.Screen))
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"Screen not specified.");
                await Connection.ShowAlert();
                return;
            }

            if (settings.AppSpecific && String.IsNullOrWhiteSpace(settings.ApplicationName))
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"Application not specified.");
                await Connection.ShowAlert();
                return;
            }

            if (String.IsNullOrWhiteSpace(settings.XPosition) || String.IsNullOrWhiteSpace(settings.YPosition))
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"X or Y position not specified.");
                await Connection.ShowAlert();
                return;
            }


            if (!int.TryParse(settings.XPosition, out int xPosition))
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"Invalid X position: {settings.XPosition}");
                await Connection.ShowAlert();
                return;
            }

            if (!int.TryParse(settings.YPosition, out int yPosition))
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"Invalid Y position: {settings.YPosition}");
                await Connection.ShowAlert();
                return;
            }

            WindowSize windowSize = null;
            WindowResize windowResize = WindowResize.NoResize;
            if (settings.ResizeWindow)
            {
                windowResize = WindowResize.ResizeWindow;

                if (String.IsNullOrWhiteSpace(settings.Height) || String.IsNullOrWhiteSpace(settings.Width))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"Height or Width position not specified.");
                    await Connection.ShowAlert();
                    return;
                }

                if (!int.TryParse(settings.Height, out int height))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"Invalid height: {settings.Height}");
                    await Connection.ShowAlert();
                    return;
                }

                if (!int.TryParse(settings.Width, out int width))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"Invalid width: {settings.Width}");
                    await Connection.ShowAlert();
                    return;
                }

                windowSize = new WindowSize(height, width);
            }
            else if (settings.MaximizeWindow)
            {
                windowResize = WindowResize.Maximize;
            }
            else if (settings.MinimizeWindow)
            {
                windowResize = WindowResize.Minimize;
            }
            else if (settings.OnlyTopmost)
            {
                windowResize = WindowResize.OnlyTopmost;
            }

            var screen = Screen.AllScreens.Where(s => s.DeviceName == settings.Screen).FirstOrDefault();
            if (screen == null)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Could not find screen {settings.Screen}");
                await Connection.ShowAlert();
                return;
            }

            var processCount = WindowPosition.MoveProcess(new MoveProcessSettings() {  AppSpecific = settings.AppSpecific,
                                                                                       Name = settings.ApplicationName,
                                                                                       DestinationScreen = screen,
                                                                                       Position = new System.Drawing.Point(xPosition, yPosition),
                                                                                       WindowResize = windowResize,
                                                                                       WindowSize = windowSize,
                                                                                       MakeTopmost = settings.TopmostWindow,
                                                                                       LocationFilter = settings.ShouldFilterLocation ? settings.LocationFilter : null,
                                                                                       TitleFilter = settings.ShouldFilterTitle ? settings.TitleFilter : null});

            if (processCount > 0)
            {
                tmrRetryProcess.Stop();
            }
            else if (processCount == 0 && !tmrRetryProcess.Enabled)
            {
                if  (!Int32.TryParse(settings.RetryAttempts, out retryAttempts))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"Invalid RetryAttempts: {settings.RetryAttempts}");
                    return;
                }
                tmrRetryProcess.Start();
            }
        }

        private async Task FetchWindowLocation()
        {
            if (string.IsNullOrEmpty(settings.ApplicationName))
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"FetchWindowLocation called with no application selected");
                await Connection.ShowAlert();
                return;
            }
            Logger.Instance.LogMessage(TracingLevel.INFO, $"FetchWindowLocation called");
            var rect = WindowPosition.GetWindowPostion(settings.ApplicationName);
            if (!rect.IsEmpty)
            {
                Logger.Instance.LogMessage(TracingLevel.DEBUG, $"Rect is X: {rect.Top} Height: {rect.Bottom} Y: {rect.Left} Width: {rect.Right}");
                settings.XPosition = rect.Left.ToString();
                settings.YPosition = rect.Top.ToString();
                settings.Height = rect.Height.ToString();
                settings.Width = rect.Width.ToString();

                // Reset to first screen
                settings.Screen = null;
                PopulateScreens();
                await SaveSettings();
            }
        }

        private async void StreamDeckConnection_OnSendToPlugin(object sender, streamdeck_client_csharp.StreamDeckEventReceivedEventArgs<streamdeck_client_csharp.Events.SendToPluginEvent> e)
        {
            var payload = e.Event.Payload;
            if (Connection.ContextId != e.Event.Context)
            {
                return;
            }

            if (payload["property_inspector"] != null)
            {
                switch (payload["property_inspector"].ToString().ToLower())
                {
                    case "getwindowdetails":
                        await FetchWindowLocation();
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


        #endregion
    }
}