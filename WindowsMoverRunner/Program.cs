using BarRaider.SdTools;
using BarRaider.WindowsMover.Internal;
using BarRaider.WindowsMover.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsMoverRunner
{
    class Program
    {
        private static string WINDOWS_MOVER_RUNNER_CONFIG = "WindowsMoverRunner.cfg";

        static void Main(string[] args)
        {
            if (!File.Exists(WINDOWS_MOVER_RUNNER_CONFIG))
            {

                Logger.Instance.LogMessage(TracingLevel.ERROR, $"WindowsMoverRunner missing configuration file");
                return;
            }

            string data = String.Empty;
            try
            {
                data = File.ReadAllText(WINDOWS_MOVER_RUNNER_CONFIG);
                MoveProcessSettings settings = JsonConvert.DeserializeObject<MoveProcessSettings>(data);
                if (settings == null)
                {
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"WindowsMoverRunner Invalid configuration data: {data}");
                    return;
                }
                Logger.Instance.LogMessage(TracingLevel.INFO, $"WindowsMoverRunner moving app: {settings.Name}");
                WindowPosition.MoveProcess(settings);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"WindowsMoverRunner exception: {ex}\nArguments: {data}");
            }
        }
    }
}
