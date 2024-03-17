using BarRaider.SdTools;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BarRaider.WindowsMover.Backend
{
    internal class UpdateHandler : IUpdateHandler
    {
        #region Private Members

        private const string UPDATE_REQUIRED_IMAGE = @"data:image/png;base64,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";
        private const string PLUGIN_UPDATE_API_URI = "https://api.barraider.com/v3/CheckVersion";
        private const int AUTO_UPDATE_COOLDOWN_MINUTES = 360;
        private const int VERSION_CHECK_COOLDOWN_SEC = 3600;
        private readonly Random rand = new Random();
        private readonly System.Timers.Timer tmrPeriodicUpdateCheck = new System.Timers.Timer();
        private DateTime lastVersionCheck = DateTime.MinValue;
        private string pluginName;
        private string pluginVersion;
        private bool shownURL = false;

        #endregion

        #region Public Methods

        public UpdateHandler()
        {
            tmrPeriodicUpdateCheck.Elapsed += TmrPeriodicUpdateCheck_Elapsed;
            tmrPeriodicUpdateCheck.Interval = TimeSpan.FromMinutes(AUTO_UPDATE_COOLDOWN_MINUTES).TotalMilliseconds;
            tmrPeriodicUpdateCheck.Start();
        }

        public bool IsBlockingUpdate { get; private set; } = false;

        public event EventHandler<PluginUpdateInfo> OnUpdateStatusChanged;

        public void SetPluginConfiguration(string pluginName, string pluginVersion)
        {
            this.pluginName = pluginName;
            this.pluginVersion = pluginVersion;
        }


        public void CheckForUpdate()
        {
            Task.Run(async () =>
            {
                // Add delay to reduce checking on plugin startup
                int delay = rand.Next(15, 40) * 1000;
                Thread.Sleep(delay);
                await PerformUpdateCheck();
            });
        }

        public void SetGlobalSettings(object settings)
        {
        }

        public void Dispose()
        {
            tmrPeriodicUpdateCheck.Stop();
        }

        #endregion

        #region Private Methods

        private async Task PerformUpdateCheck()
        {
            // Don't check if already requires update
            if (IsBlockingUpdate)
            {
                return;
            }

            if ((DateTime.Now - lastVersionCheck).TotalSeconds < VERSION_CHECK_COOLDOWN_SEC)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} *UC* in cooldown.");
                return;
            }

            try
            {
                lastVersionCheck = DateTime.Now;

                using HttpClient client = new HttpClient
                {
                    Timeout = new TimeSpan(0, 0, 15)
                };

                Dictionary<string, string> dic = new Dictionary<string, string>
                {
                    ["plugin"] = pluginName,
                    ["version"] = pluginVersion,
                    ["clientId"] = getClientId(),
                    ["requestDate"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                var content = new StringContent(JsonConvert.SerializeObject(dic), Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync(PLUGIN_UPDATE_API_URI, content);
                var result = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} *UC* Error: {response.StatusCode} Reason: {response.ReasonPhrase} Body: {result}");
                    return;
                }


                Logger.Instance.LogMessage(TracingLevel.INFO, $"[{Thread.CurrentThread.ManagedThreadId}] *UC* complete");
                var updateResponse = JsonConvert.DeserializeObject<UpdateCheckResponse>(result);
                HandleUpdateResponse(updateResponse);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"[{Thread.CurrentThread.ManagedThreadId}] *UC* Exception: {ex}");
                return;
            }
        }

        private static string identifier(string wmiClass, string wmiProperty, string wmiMustBeTrue)
        {
            string result = "";
            System.Management.ManagementClass mc = new System.Management.ManagementClass(wmiClass);
            System.Management.ManagementObjectCollection moc = mc.GetInstances();
            foreach (System.Management.ManagementObject mo in moc)
            {
                if (mo[wmiMustBeTrue].ToString() == "True")
                {
                    //Only get the first one
                    if (result == "")
                    {
                        try
                        {
                            result = mo[wmiProperty].ToString();
                            break;
                        }
                        catch
                        {
                        }
                    }
                }
            }
            return result;
        }
        //Return a hardware identifier
        private static string identifier(string wmiClass, string wmiProperty)
        {
            string result = "";
            System.Management.ManagementClass mc = new System.Management.ManagementClass(wmiClass);
            System.Management.ManagementObjectCollection moc = mc.GetInstances();
            foreach (System.Management.ManagementObject mo in moc)
            {
                //Only get the first one
                if (result == "")
                {
                    try
                    {
                        result = mo[wmiProperty].ToString();
                        break;
                    }
                    catch
                    {
                    }
                }
            }
            return result;
        }

        private string getClientId()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(System.Security.Principal.WindowsIdentity.GetCurrent().Name + "|");
            sb.Append(identifier("Win32_Processor", "ProcessorId"));

            var byteData = Encoding.UTF8.GetBytes(sb.ToString());
            var hashData = SHA256.Create().ComputeHash(byteData);
            StringBuilder resultBuilder = new StringBuilder();

            for (int i = 0; i < hashData.Length; i++)
            {
                resultBuilder.Append(hashData[i].ToString("x2"));
            }
            return resultBuilder.ToString();
        }

        private void TmrPeriodicUpdateCheck_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            CheckForUpdate();
        }

        private void HandleUpdateResponse(UpdateCheckResponse response)
        {
            string url = null;
            string image = null;

            if (response == null)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"[{Thread.CurrentThread.ManagedThreadId}] *UC* Response is null!");
                return;
            }

            if (response.Status == PluginUpdateStatus.UpToDate)
            {
                Logger.Instance.LogMessage(TracingLevel.INFO, $"[{Thread.CurrentThread.ManagedThreadId}] Plugin version is up to date");
                return;
            }


            if (response.Status == PluginUpdateStatus.Unknown)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"[{Thread.CurrentThread.ManagedThreadId}] *UC* Response status: {response.Status}");
                return;
            }

            if (response.Status == PluginUpdateStatus.MajorUpgrade || response.Status == PluginUpdateStatus.CriticalUpgrade)
            {
                shownURL = false;
                url = response.UpdateURL;
                image = UPDATE_REQUIRED_IMAGE;
                IsBlockingUpdate = true;
            }
            else if (!shownURL && !String.IsNullOrEmpty(response.UpdateURL))
            {
                shownURL = true; // Only show once when the plugin loads
                url = response.UpdateURL;
            }

            Logger.Instance.LogMessage(TracingLevel.WARN, $"Plugin update is required");
            OnUpdateStatusChanged?.Invoke(this, new PluginUpdateInfo(response.Status, url, image));
        }

        private class UpdateCheckResponse
        {
            /// <summary>
            /// Status
            /// </summary>
            [JsonProperty("status")]
            public PluginUpdateStatus Status { get; private set; }

            /// <summary>
            /// Update URL
            /// </summary>
            [JsonProperty("updateURL")]
            public string UpdateURL { get; private set; }
        }

        #endregion
    }
}
