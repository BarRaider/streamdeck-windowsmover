using BarRaider.WindowsMover.Internal;
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
        public bool AppSpecific { get; set; }
        public string Name { get; set; }

        public Screen DestinationScreen { get; set; }

        public Point Position { get; set; }

        public WindowResize WindowResize { get; set; }

        public WindowSize WindowSize { get; set; }

        public bool MakeTopmost { get; set; }

        public string LocationFilter { get; set; }

        public string TitleFilter { get; set; }

        public override string ToString()
        {
            return $"AppSpecific: {AppSpecific} ProcessName: {Name} DestinationScreen: {DestinationScreen?.DeviceName} Position: {Position.X},{Position.Y} Resize: {WindowResize} Height: {WindowSize?.Height} Width: {WindowSize?.Width} TopMost: {MakeTopmost} LocationFilter: {LocationFilter} TitleFilter: {TitleFilter}";

        }
    }
}
