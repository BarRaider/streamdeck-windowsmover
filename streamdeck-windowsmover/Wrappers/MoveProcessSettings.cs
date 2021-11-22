using BarRaider.WindowsMover.Internal;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarRaider.WindowsMover.Wrappers
{
    public class MoveProcessSettings
    {
        [JsonProperty(PropertyName = "foregroundHandle")]
        public int ForegroundHandle { get; set; }
        
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "destinationScreenName")]
        public string DestinationScreenDeviceName { get; set; }

        [JsonProperty(PropertyName = "position")]
        public Point Position { get; set; }

        [JsonProperty(PropertyName = "windowResize")]
        public WindowResize WindowResize { get; set; }

        [JsonProperty(PropertyName = "windowSize")]
        public WindowSize WindowSize { get; set; }

        [JsonProperty(PropertyName = "makeTopmost")]
        public bool MakeTopmost { get; set; }

        [JsonProperty(PropertyName = "locationFilter")]
        public string LocationFilter { get; set; }

        [JsonProperty(PropertyName = "titleFilter")]
        public string TitleFilter { get; set; }

        public override string ToString()
        {
            return $"ForegroundHandle: {ForegroundHandle} ProcessName: {Name} DestinationScreen: {DestinationScreenDeviceName} Position: {Position.X},{Position.Y} Resize: {WindowResize} Height: {WindowSize?.Height} Width: {WindowSize?.Width} TopMost: {MakeTopmost} LocationFilter: {LocationFilter} TitleFilter: {TitleFilter}";

        }
    }
}
