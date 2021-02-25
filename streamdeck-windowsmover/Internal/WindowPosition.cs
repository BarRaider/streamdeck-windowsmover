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

namespace BarRaider.WindowsMover.Internal
{
    public enum WindowResize
    {
        NoResize = 0,
        Maximize = 1,
        Minimize = 2,
        ResizeWindow = 3,
        OnlyTopmost = 4
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

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        public static int MoveProcess(MoveProcessSettings settings)
        {
            if (settings == null)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, "MoveProcess called with null settings");
                return 0;
            }

            // Set Resize
            SetWindowPosFlags flags = SetWindowPosFlags.SWP_SHOWWINDOW | SetWindowPosFlags.SWP_NOZORDER;
            int height = 0;
            int width = 0;
            if (settings.WindowResize != WindowResize.ResizeWindow || settings.WindowSize == null)
            {
                flags |= SetWindowPosFlags.SWP_NOSIZE;
            }
            else
            {
                height = settings.WindowSize.Height;
                width = settings.WindowSize.Width;
            }

            Logger.Instance.LogMessage(TracingLevel.INFO, $"Changing window position for: {settings}");
            if (!settings.AppSpecific)
            {
                ManipulateWindow(GetForegroundWindow(), settings, width, height, flags);
                return 1;
            }
            else
            {
                int totalProcesses = 0;
                int movedProcesses = 0;
                foreach (var process in System.Diagnostics.Process.GetProcessesByName(settings.Name))
                {
                    try
                    {
                        totalProcesses++;
                        IntPtr h1 = process.MainWindowHandle;
                        if (h1.ToInt32() == 0)
                        {
                            continue;
                        }
                        Logger.Instance.LogMessage(TracingLevel.INFO, $"Found {settings.Name} with handle {h1}");

                        if (!String.IsNullOrEmpty(settings.LocationFilter) && !process.MainModule.FileName.ToLowerInvariant().Contains(settings.LocationFilter.ToLowerInvariant()))
                        {
                            Logger.Instance.LogMessage(TracingLevel.INFO, $"Skipped {settings.Name} with handle {h1} as the file location was different from \"{settings.LocationFilter}\": {process.MainModule.FileName}");
                            continue;
                        }

                        if (!String.IsNullOrEmpty(settings.TitleFilter) && !process.MainWindowTitle.Contains(settings.TitleFilter))
                        {
                            Logger.Instance.LogMessage(TracingLevel.INFO, $"Skipped {settings.Name} with handle {h1} as the window title was different from \"{settings.TitleFilter}\": {process.MainWindowTitle}");
                            continue;
                        }
                        movedProcesses++;

                        ManipulateWindow(h1, settings, width, height, flags);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error setting window for process {settings.Name} {ex}");
                    }
                }
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Iterated through {totalProcesses} processes, moved {movedProcesses}");

                return movedProcesses;
            }
        }

        private static void ManipulateWindow(IntPtr windowHandle, MoveProcessSettings settings, int width, int height, SetWindowPosFlags flags)
        {
            // Needed to support multi-maximize clicks
            if (settings.WindowResize != WindowResize.Minimize && settings.WindowResize != WindowResize.OnlyTopmost)
            {
                ShowWindow(windowHandle, ShowWindowEnum.SHOWNORMAL);
            }

            // Do not change window position or location in the "OnlyTopmost" setting is set
            if (settings.WindowResize != WindowResize.OnlyTopmost)
            {
                // Resize and move window
                SetWindowPos(windowHandle, new IntPtr(0), settings.DestinationScreen.WorkingArea.X + settings.Position.X, settings.DestinationScreen.WorkingArea.Y + settings.Position.Y, width, height, flags);
            }

            if (settings.WindowResize == WindowResize.Maximize)
            {
                // Maximize the window.
                ShowWindow(windowHandle, ShowWindowEnum.SHOWMAXIMIZED);
            }
            else if (settings.WindowResize == WindowResize.Minimize)
            {
                // Minimize the window.
                ShowWindow(windowHandle, ShowWindowEnum.MINIMIZE);
            }
            SetForegroundWindow(windowHandle);

            if (settings.MakeTopmost)
            {
                try
                {
                    TryForceForegroundWindow(windowHandle);
                }
                catch (Exception ex)
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"Failed to make topmost {settings.Name} {ex}");
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

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool LockSetForegroundWindow(uint uLockCode);

        [DllImport("user32.dll")]
        static extern bool AllowSetForegroundWindow(int dwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetFocus(IntPtr hWnd);

        static readonly uint LSFW_UNLOCK = 2;

        static readonly int ASFW_ANY = -1; // by MSDN

        private static void TryForceForegroundWindow(IntPtr hWnd)
        {
            LockSetForegroundWindow(LSFW_UNLOCK);
            AllowSetForegroundWindow(ASFW_ANY);

            IntPtr hWndForeground = GetForegroundWindow();
            if (hWndForeground.ToInt32() != 0)
            {
                if (hWndForeground != hWnd)
                {
                    uint thread1 = GetWindowThreadProcessId(hWndForeground, out _);
                    uint thread2 = GetCurrentThreadId();


                    if (thread1 != thread2)
                    {
                        AttachThreadInput(thread1, thread2, true);
                        LockSetForegroundWindow(LSFW_UNLOCK);
                        AllowSetForegroundWindow(ASFW_ANY);
                        BringWindowToTop(hWnd);
                        if (IsIconic(hWnd))
                        {
                            ShowWindow(hWnd, ShowWindowEnum.SHOWNORMAL);
                        }
                        else
                        {
                            ShowWindow(hWnd, ShowWindowEnum.SHOW);
                        }
                        SetFocus(hWnd);
                        AttachThreadInput(thread1, thread2, false);
                    }
                    else
                    {
                        AttachThreadInput(thread1, thread2, true);
                        LockSetForegroundWindow(LSFW_UNLOCK);
                        AllowSetForegroundWindow(ASFW_ANY);
                        BringWindowToTop(hWnd);
                        SetForegroundWindow(hWnd);
                        SetFocus(hWnd);
                        AttachThreadInput(thread1, thread2, false);

                    }
                    if (IsIconic(hWnd))
                    {
                        AttachThreadInput(thread1, thread2, true);
                        LockSetForegroundWindow(LSFW_UNLOCK);
                        AllowSetForegroundWindow(ASFW_ANY);
                        BringWindowToTop(hWnd);
                        ShowWindow(hWnd, ShowWindowEnum.SHOWNORMAL);
                        SetFocus(hWnd);
                        AttachThreadInput(thread1, thread2, false);
                    }
                    else if (IsZoomed(hWnd))
                    {
                        BringWindowToTop(hWnd);
                        ShowWindow(hWnd, ShowWindowEnum.SHOWMAXIMIZED);
                    }
                    else
                    {
                        BringWindowToTop(hWnd);
                        ShowWindow(hWnd, ShowWindowEnum.SHOW);
                    }
                }
                SetForegroundWindow(hWnd);
                SetFocus(hWnd);
            }
            else
            {
                uint thread1 = GetWindowThreadProcessId(hWndForeground, out _);
                uint thread2 = GetCurrentThreadId();
                try
                {
                    AttachThreadInput(thread1, thread2, true);
                }
                catch
                {
                }
                LockSetForegroundWindow(LSFW_UNLOCK);
                AllowSetForegroundWindow(ASFW_ANY);
                BringWindowToTop(hWnd);
                SetForegroundWindow(hWnd);

                ShowWindow(hWnd, ShowWindowEnum.SHOW);
                SetFocus(hWnd);
                AttachThreadInput(thread1, thread2, false);
            }
        }
    }
}
