using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarRaider.WindowsMover.Internal
{
    internal static class ScreenFinder
    {
        internal static Screen FromDeviceName(string deviceName)
        {
            var screen = Screen.AllScreens.FirstOrDefault(s => s.DeviceName == deviceName);
            if (screen == null)
            {
                Logger.Instance.LogMessage(TracingLevel.WARN, $"Failed to find screen based on deviceName: {deviceName}");
            }
            return screen;
        }
    }
}
