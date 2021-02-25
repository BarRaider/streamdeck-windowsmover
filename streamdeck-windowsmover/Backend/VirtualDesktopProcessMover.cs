using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtualDesktop;

namespace BarRaider.WindowsMover.Backend
{
    internal static class VirtualDesktopProcessMover
    {
        public static int MoveProcess(VirtualDesktopProcessMoverSettings settings)
        {
            if (settings == null)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"MoveProcess called but settings is null!");
                return 0;
            }

            if (String.IsNullOrEmpty(settings.VirtualDesktopName))
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"MoveProcess called but VirtualDesktopName is null!");
                return 0;
            }

            try
            {
                // Check if the virtual desktop exists
                int id = Desktop.SearchDesktop(settings.VirtualDesktopName);
                if (id < 0)
                {
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"Virtual desktop with name {settings.VirtualDesktopName} does not exist");
                    return 0;
                }

                var desktop = Desktop.FromIndex(id);
                if (desktop == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"Failed to retrieve Virtual Desktop with id {id}");
                    return 0;
                }

                // We found the relevant virtual desktop, now lets find the app to move
                Logger.Instance.LogMessage(TracingLevel.INFO, $"Changing virtual desktop for: {settings}");
                if (!settings.AppSpecific) // Move the current window
                {
                    desktop.MoveActiveWindow();
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
                            desktop.MoveWindow(h1);
                        }
                        catch (Exception ex)
                        {
                            Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error setting Virtual Desktop for process {settings.Name} {ex}");
                        }
                    }
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"Iterated through {totalProcesses} processes, moved {movedProcesses}");

                    return movedProcesses;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"MoveProcess Exception {ex}");
            }
            return 0;
        }
    }
}
