using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarRaider.WindowsMover.Wrappers
{
    public class WindowSize
    {
        public int Height { get; private set; }
        public int Width { get; private set; }

        public WindowSize(int height, int width)
        {
            Height = height;
            Width = width;
        }
    }
}
