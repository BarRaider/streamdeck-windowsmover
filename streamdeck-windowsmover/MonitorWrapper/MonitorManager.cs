using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BarRaider.WindowsMover.Internal;

namespace BarRaider.WindowsMover.MonitorWrapper
{
    internal class MonitorManager
    {
        #region Private Members
        private const int MONITORS_REFRESH_SECONDS = 300; // 5 minutes

        private static MonitorManager instance = null;
        private static readonly object objLock = new object();

        

        List<MonitorInfo> monitors = null;
        DateTime lastMonitorUpdate = DateTime.MinValue;

        #endregion

        #region Constructors

        public static MonitorManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                lock (objLock)
                {
                    if (instance == null)
                    {
                        instance = new MonitorManager();
                    }
                    return instance;
                }
            }
        }

        private MonitorManager()
        {
            
        }

        #endregion

        #region Public Methods

        public List<MonitorInfo> GetAllMonitors()
        {
            RefreshMonitors();
            return monitors;
        }

        public string GetScreenDeviceNameFromUniqueValue(string uniqueValue)
        {
            if (string.IsNullOrEmpty(uniqueValue))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"GetScreenFromUniqueValue empty uniqueValue");
                return null;
            }

            string[] values = uniqueValue.Split(Constants.UNIQUE_VALUE_DELIMITER);
            if (values.Length != 3)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"GetScreenFromUniqueValue Invalid screen uniqueValue: {uniqueValue}");
                return null;
            }

            RefreshMonitors();
            if (HasUniqueFriendlyName())
            {
                var monitor = monitors.FirstOrDefault(mon => mon.WMIInfo?.UserFriendlyName == values[0]);
                if (monitor != null)
                {
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"Found monitor based on unique friendly name");
                    return monitor.DeviceName;
                }
            }

            if (HasUniqueSerial())
            {
                var monitor = monitors.FirstOrDefault(mon => mon.WMIInfo?.SerialNumber == values[1]);
                if (monitor != null)
                {
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"Found monitor based on unique serial");
                    return monitor.DeviceName;
                }
            }

            if (HasUniqueInstanceName())
            {
                var monitor = monitors.FirstOrDefault(mon => mon.WMIInfo?.InstanceName == values[2]);
                if (monitor != null)
                {
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"Found monitor based on unique instance name");
                    return monitor.DeviceName;
                }
            }

            Logger.Instance.LogMessage(TracingLevel.WARN, $"Failed to find screen based on uniqueValue: {uniqueValue}");
            return null;
        }

        public bool HasUniqueSerial()
        {
            Dictionary<string, int> dicMonitorSerials = new Dictionary<string, int>();
            RefreshMonitors();
            foreach (var monitor in monitors)
            {
                if (dicMonitorSerials.ContainsKey(monitor.WMIInfo.SerialNumber))
                {
                    return false;
                }
                dicMonitorSerials[monitor.WMIInfo.SerialNumber] = 1;
            }
            return true;
        }

        public bool HasUniqueFriendlyName()
        {
            Dictionary<string, int> dicMonitorNames = new Dictionary<string, int>();
            RefreshMonitors();
            foreach (var monitor in monitors)
            {
                if (dicMonitorNames.ContainsKey(monitor.WMIInfo.UserFriendlyName))
                {
                    return false;
                }
                dicMonitorNames[monitor.WMIInfo.UserFriendlyName] = 1;
            }
            return true;
        }

        public bool HasUniqueInstanceName()
        {
            Dictionary<string, int> dicMonitorNames = new Dictionary<string, int>();
            RefreshMonitors();
            foreach (var monitor in monitors)
            {
                if (dicMonitorNames.ContainsKey(monitor.WMIInfo.InstanceName))
                {
                    return false;
                }
                dicMonitorNames[monitor.WMIInfo.InstanceName] = 1;
            }
            return true;
        }

        #endregion

        #region Private Methods

        private void RefreshMonitors()
        {
            try
            {
                if (monitors == null || (DateTime.Now - lastMonitorUpdate).TotalSeconds > MONITORS_REFRESH_SECONDS)
                {
                    monitors = AggregatedMonitorInfo.GetAggregatedMonitorInfo();
                    if (monitors != null)
                    {
                        lastMonitorUpdate = DateTime.Now;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"RefreshMonitors exception {ex}");
            }
        }

        #endregion
    }
}
