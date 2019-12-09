namespace BarRaider.WindowsMover.MonitorWrapper
{
    class WMIMonitorInfo
    {
        public bool IsActive { get; private set; }
        public string InstanceName { get; private set; }
        public string UserFriendlyName { get; private set; }
        public string Manufacturer { get; private set; }
        public string ProductCode { get; private set; }
        public string SerialNumber { get; private set; }
        public int YearOfManufacture { get; private set; }

        public WMIMonitorInfo(bool isActive, string instanceName, string userFriendlyName, string manufacturer, string productCode, string serialNumber, int manufactureYear)
        {
            IsActive = isActive;
            InstanceName = instanceName;
            UserFriendlyName = userFriendlyName;
            Manufacturer = manufacturer;
            ProductCode = productCode;
            SerialNumber = serialNumber;
            YearOfManufacture = manufactureYear;
        }

        public override string ToString()
        {
            return $"UserFriendlyName: {UserFriendlyName}, IsActive: {IsActive}, Serial: {SerialNumber}, Manufacturer: {Manufacturer}, ProductCode: {ProductCode}, InstanceName: {InstanceName}, Year: {YearOfManufacture}";
        }
    }
}
