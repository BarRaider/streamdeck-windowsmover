using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarRaider.WindowsMover.Wrappers
{
    public class ProcessInfo
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        public ProcessInfo(string processName)
        {
            Name = processName;
        }
    }
}
