using System;
using System.Diagnostics;
using System.Threading;

namespace GameMemoryWizard.Services {
    public class MenuService {

        public static string RetrieveResponse(string command) {
            Console.WriteLine(command);
            return Console.ReadLine();
        }

        public static void DisplayWelcome() {
            string welcomeMessage = "Welcome to the Game Memory Wizard (Reader).\r\n";
            Console.WriteLine(welcomeMessage);

            string processName = RetrieveProcessName();
        }

        public static string RetrieveProcessName() {
            int retryCount = 0;
            var maxRetries = 20;

            while (retryCount < maxRetries) {
                string processName = null;
                try {
                    processName = RetrieveResponse("Please enter the process name of the game you wish to modify the memory for:");
                    Process[] processes = Process.GetProcessesByName(processName);
                    if (processes.Length == 0) {
                        throw new ApplicationException($"Could not find process of name: {processName}\r\n");
                    }
                    return processName;
                } catch (Exception exception) {
                    retryCount++;
                    Console.WriteLine(exception.Message);
                }
            }
            Console.WriteLine($"Could not find process. Terminating App...");
            Thread.Sleep(5000);
            throw new ApplicationException("Unable to find process");
        }
    }
}
