using Newtonsoft.Json;

namespace BarRaider.WindowsMover.MonitorWrapper
{
    class MonitorInfo
    {
        public WMIMonitorInfo WMIInfo { get; private set; }
        public QueryMonitorInfo QueryInfo { get; private set; }

        [JsonProperty(PropertyName = "name")]
        public string DisplayName { get; set; }

        [JsonProperty(PropertyName = "uniqueValue")]
        public string UniqueValue { get; private set; }

        public string FriendlyName { get; private set; }

        [JsonProperty(PropertyName = "device")]
        public string DeviceName { get; private set; }

        public MonitorInfo(WMIMonitorInfo wmiInfo, QueryMonitorInfo queryInfo)
        {
            WMIInfo = wmiInfo;
            QueryInfo = queryInfo;

            if (WMIInfo != null && QueryInfo != null)
            {
                UniqueValue = $"{WMIInfo?.UserFriendlyName}{Constants.UNIQUE_VALUE_DELIMITER}{WMIInfo?.SerialNumber}{Constants.UNIQUE_VALUE_DELIMITER}{WMIInfo.InstanceName}";
                DeviceName = QueryInfo?.DisplayName;
                FriendlyName = WMIInfo?.UserFriendlyName;
                DisplayName = FriendlyName;
            }
        }

        public override string ToString()
        {
            return $"WMI: {WMIInfo.ToString()}\nQUERY: {QueryInfo.ToString()}";
        }
    }
}
