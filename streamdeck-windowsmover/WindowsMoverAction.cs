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
                    ScreenFriendlyName = true
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


        }

        #region Private Members

        private readonly PluginSettings settings;

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
            if (!String.IsNullOrEmpty(settings.ApplicationName))
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

            if (String.IsNullOrWhiteSpace(settings.ApplicationName))
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

            var screen = Screen.AllScreens.Where(s => s.DeviceName == settings.Screen).FirstOrDefault();
            if (screen == null)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Could not find screen {settings.Screen}");
                await Connection.ShowAlert();
                return;
            }

            WindowPosition.MoveProcess(settings.ApplicationName, screen, new System.Drawing.Point(xPosition, yPosition), windowResize, windowSize);
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

        #endregion
    }
}