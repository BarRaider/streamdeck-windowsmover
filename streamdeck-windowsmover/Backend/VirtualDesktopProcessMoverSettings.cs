using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarRaider.WindowsMover.Backend
{
    internal class VirtualDesktopProcessMoverSettings
    {
        public bool AppSpecific { get; set; }
        public string Name { get; set; }

        public string LocationFilter { get; set; }

        public string TitleFilter { get; set; }

        public string VirtualDesktopName { get; set; }

        public override string ToString()
        {
            return $"AppSpecific: {AppSpecific} ProcessName: {Name} Destination: {VirtualDesktopName} LocationFilter: {LocationFilter} TitleFilter: {TitleFilter}";

        }
    }
}
