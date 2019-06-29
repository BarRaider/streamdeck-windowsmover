using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarRaider.WindowsMover.Wrappers
{
    class ScreenInfo
    {
        [JsonProperty(PropertyName = "name")]
        public string FriendlyName { get; set; }

        [JsonProperty(PropertyName = "device")]
        public string DeviceName { get; set; }

        public ScreenInfo(string deviceName, string friendlyName)
        {
            DeviceName = deviceName;
            FriendlyName = friendlyName;
        }
    }
}
