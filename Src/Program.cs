
using System.ServiceProcess;

namespace winCronNamespace
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            StartProgram();
        }

        private static void StartProgram()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] { new winCron() };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
