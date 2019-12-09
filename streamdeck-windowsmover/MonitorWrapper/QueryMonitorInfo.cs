namespace BarRaider.WindowsMover.MonitorWrapper
{
    class QueryMonitorInfo
    {
        public LUID AdapterId { get; set; }

        public uint Id { get; set; }

        public string DevicePath { get; set; }

        public string DisplayName { get; set; }

        public string UserFriendlyName { get; set; }

        public override string ToString()
        {
            return $"UserFriendlyName: {UserFriendlyName}, DisplayName: {DisplayName}, AdapterId: {AdapterId.HighPart}-{AdapterId.LowPart}, Id: {Id}, DevicePath: {DevicePath}";
        }
    }
}
