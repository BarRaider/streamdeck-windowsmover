﻿using BarRaider.SdTools;
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
                PluginSettings instance = new PluginSettings();
                instance.ApplicationName = String.Empty;
                instance.Screen = String.Empty;
                instance.Height = "900";
                instance.Width = "1500";
                instance.XPosition = "0";
                instance.YPosition = "0";
                instance.NoResizeWindow = true;
                instance.ResizeWindow = false;
                instance.MaximizeWindow = false;
                instance.MinimizeWindow = false;

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

            [JsonProperty(PropertyName = "applications")]
            public List<ProcessInfo> Applications { get; set; }

            [JsonProperty(PropertyName = "screens")]
            public List<ScreenInfo> Screens { get; set; }


        }

        #region Private Members

        private PluginSettings settings;

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
            PopulateApplications();
            PopulateScreens();
            SaveSettings();
        }

        public override void Dispose()
        {
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
            Tools.AutoPopulateSettings(settings, payload.Settings);
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
            settings.Applications = System.Diagnostics.Process.GetProcesses().Select(p => new ProcessInfo(p.ProcessName)).GroupBy(p=>p.Name).Select(p=>p.First()).OrderBy(p => p.Name).ToList();
            if (string.IsNullOrEmpty(settings.ApplicationName) && settings.Applications.Count > 0)
            {
                settings.ApplicationName = settings.Applications[0].Name;
            }
        }

        private void PopulateScreens()
        {
            settings.Screens = Screen.AllScreens.Select(s =>
            {
                string friendlyName = s.DeviceName;
                try
                {
                    string friendlyNameStr = s.DeviceFriendlyName();
                    if (!String.IsNullOrEmpty(friendlyNameStr))
                    {
                        friendlyName = friendlyNameStr;
                    }
                }
                catch { }
                return new ScreenInfo(s.DeviceName, friendlyName);
               }).ToList();

            if (string.IsNullOrWhiteSpace(settings.Screen) && settings.Screens.Count > 0)
            {
                settings.Screen = settings.Screens[0].DeviceName;
            }
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

            int xPosition;
            int yPosition;
            int height;
            int width;

            if (!int.TryParse(settings.XPosition, out xPosition))
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"Invalid X position: {settings.XPosition}");
                await Connection.ShowAlert();
                return;
            }

            if (!int.TryParse(settings.YPosition, out yPosition))
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

                if (!int.TryParse(settings.Height, out height))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"Invalid height: {settings.Height}");
                    await Connection.ShowAlert();
                    return;
                }

                if (!int.TryParse(settings.Width, out width))
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

        #endregion
    }
}