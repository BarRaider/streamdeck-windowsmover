using BarRaider.SdTools;
using BarRaider.WindowsMover.Wrappers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BarRaider.WindowsMover
{
    public enum WindowResize
    {
        NoResize = 0,
        Maximize = 1,
        Minimize = 2,
        ResizeWindow = 3
    }

    public enum ShowWindowEnum : int
    {
        HIDE = 0,
        SHOWNORMAL = 1,
        SHOWMINIMIZED = 2,
        SHOWMAXIMIZED = 3,
        SHOWNOACTIVATE = 4,
        SHOW = 5,
        MINIMIZE = 6,
        SHOWMINNOACTIVE = 7,
        SHOWNA = 8,
        RESTORE = 9,
        SHOWDEFAULT = 10
    }


    [Flags]
    public enum SetWindowPosFlags : uint
    {
        // ReSharper disable InconsistentNaming

        /// <summary>
        ///     If the calling thread and the thread that owns the window are attached to different input queues, the system posts the request to the thread that owns the window. This prevents the calling thread from blocking its execution while other threads process the request.
        /// </summary>
        SWP_ASYNCWINDOWPOS = 0x4000,

        /// <summary>
        ///     Prevents generation of the WM_SYNCPAINT message.
        /// </summary>
        SWP_DEFERERASE = 0x2000,

        /// <summary>
        ///     Draws a frame (defined in the window's class description) around the window.
        /// </summary>
        SWP_DRAWFRAME = 0x0020,

        /// <summary>
        ///     Applies new frame styles set using the SetWindowLong function. Sends a WM_NCCALCSIZE message to the window, even if the window's size is not being changed. If this flag is not specified, WM_NCCALCSIZE is sent only when the window's size is being changed.
        /// </summary>
        SWP_FRAMECHANGED = 0x0020,

        /// <summary>
        ///     Hides the window.
        /// </summary>
        SWP_HIDEWINDOW = 0x0080,

        /// <summary>
        ///     Does not activate the window. If this flag is not set, the window is activated and moved to the top of either the topmost or non-topmost group (depending on the setting of the hWndInsertAfter parameter).
        /// </summary>
        SWP_NOACTIVATE = 0x0010,

        /// <summary>
        ///     Discards the entire contents of the client area. If this flag is not specified, the valid contents of the client area are saved and copied back into the client area after the window is sized or repositioned.
        /// </summary>
        SWP_NOCOPYBITS = 0x0100,

        /// <summary>
        ///     Retains the current position (ignores X and Y parameters).
        /// </summary>
        SWP_NOMOVE = 0x0002,

        /// <summary>
        ///     Does not change the owner window's position in the Z order.
        /// </summary>
        SWP_NOOWNERZORDER = 0x0200,

        /// <summary>
        ///     Does not redraw changes. If this flag is set, no repainting of any kind occurs. This applies to the client area, the nonclient area (including the title bar and scroll bars), and any part of the parent window uncovered as a result of the window being moved. When this flag is set, the application must explicitly invalidate or redraw any parts of the window and parent window that need redrawing.
        /// </summary>
        SWP_NOREDRAW = 0x0008,

        /// <summary>
        ///     Same as the SWP_NOOWNERZORDER flag.
        /// </summary>
        SWP_NOREPOSITION = 0x0200,

        /// <summary>
        ///     Prevents the window from receiving the WM_WINDOWPOSCHANGING message.
        /// </summary>
        SWP_NOSENDCHANGING = 0x0400,

        /// <summary>
        ///     Retains the current size (ignores the cx and cy parameters).
        /// </summary>
        SWP_NOSIZE = 0x0001,

        /// <summary>
        ///     Retains the current Z order (ignores the hWndInsertAfter parameter).
        /// </summary>
        SWP_NOZORDER = 0x0004,

        /// <summary>
        ///     Displays the window.
        /// </summary>
        SWP_SHOWWINDOW = 0x0040,

        // ReSharper restore InconsistentNaming
    }

    public static class WindowPosition
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        // The ShowWindowAsync method alters the windows show state through the nCmdShow parameter.
        // The nCmdShow parameter can have any of the SW values.
        // See http://msdn.microsoft.com/library/en-us/winui/winui/windowsuserinterface/windowing/windows/windowreference/windowfunctions/showwindowasync.asp
        // for full documentation.
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, ShowWindowEnum nCmdShow);

        // Get window's rect
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        public static void MoveProcess(string processName, Screen destinationScreen, Point position, WindowResize windowResize, WindowSize windowSize)
        {
            SetWindowPosFlags flags = SetWindowPosFlags.SWP_NOZORDER | SetWindowPosFlags.SWP_SHOWWINDOW;
            int height = 0;
            int width = 0;
            if (windowResize != WindowResize.ResizeWindow || windowSize == null)
            {
                flags |= SetWindowPosFlags.SWP_NOSIZE;
            }
            else
            {
                height = windowSize.Height;
                width = windowSize.Width;
            }

            Logger.Instance.LogMessage(TracingLevel.INFO, $"Changing window position for {processName} - Resize: {windowResize}");
            foreach (var process in System.Diagnostics.Process.GetProcessesByName(processName))
            {
                try
                {
                    IntPtr h1 = process.MainWindowHandle;
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"Found {processName} with handle {h1}");
                    // Needed to support multi-maximize clicks
                    if (windowResize != WindowResize.Minimize)
                    {
                        ShowWindow(h1, ShowWindowEnum.SHOWNORMAL);
                    }

                    SetWindowPos(h1, (IntPtr)0, destinationScreen.WorkingArea.X + position.X, destinationScreen.WorkingArea.Y + position.Y, width, height, flags);
                    if (windowResize == WindowResize.Maximize)
                    {
                        // Minimize the window.
                        ShowWindow(h1, ShowWindowEnum.SHOWMAXIMIZED);
                    }
                    else if (windowResize == WindowResize.Minimize)
                    {
                        // Minimize the window.
                        ShowWindow(h1, ShowWindowEnum.MINIMIZE);
                    }
                    SetForegroundWindow(h1);
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error setting window for process {processName} {ex}");
                }
            }
        }

        public static Rectangle GetWindowPostion(string processName)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"GetWindowPostion for {processName}");
            foreach (var process in System.Diagnostics.Process.GetProcessesByName(processName))
            {
                try
                {
                    IntPtr h1 = process.MainWindowHandle;
                    if (h1.ToInt32() == 0)
                    {
                        continue;
                    }
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"Found {processName} with handle {h1}");

                    RECT rct = new RECT();
                    GetWindowRect(h1, ref rct);
                    Logger.Instance.LogMessage(TracingLevel.DEBUG, $"Rect is Top: {rct.Top} Bottom: {rct.Bottom} Left: {rct.Left} Right: {rct.Right}");
                    if (rct.Bottom > rct.Top)
                    {
                        return new Rectangle(rct.Left, rct.Top, rct.Right - rct.Left, rct.Bottom - rct.Top);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"GetWindowPostion error {processName} {ex}");
                }
            }
            return Rectangle.Empty;
        }
    }
}
