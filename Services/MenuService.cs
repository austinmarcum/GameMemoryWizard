using System;
using System.Diagnostics;
using System.Threading;

namespace GameMemoryWizard.Services {
    public class MenuService {

        public static string RetrieveResponse(string command) {
            Console.WriteLine("\r\n" + command);
            return Console.ReadLine();
        }

        public static void DisplayMenu() {
            string welcomeMessage = "Welcome to the Game Memory Wizard (Reader).\r\n";
            Console.WriteLine(welcomeMessage);
            string gameName = RetrieveResponse("What game would you like to cheat in?");
            string processName = RetrieveProcessName();
            ThreadService.SetProcressName(processName);
            string cheatName = RetrieveResponse("What would you like the first cheat to be called?");
            string cheatType = RetrieveResponse("What is the cheat type (Lock, Multiplier, IncreaseTo or DecreaseTo)?");
            string cheatAmount = RetrieveResponse("What is the amount for the cheat?");
            HandleScanning();
        }

        public static void HandleScanning() {
            Console.WriteLine("\r\n");
            Console.WriteLine("To find the memory location, change the value in the game and perform the cooresponding scan.");
            while (true) {
                Console.WriteLine("1. Scan for Increase of value you wish to find (Shift + +)");
                Console.WriteLine("2. Scan for Decrease of value you wish to find (Shift + -)");
                Console.WriteLine("3. Scan for Change of value you wish to find (Shift + C)");
                Console.WriteLine("4. Scan for value that has not changed you wish to find (Shift + E)");
                string choice = Console.ReadLine();

                switch (choice) {
                    case "1":
                        ThreadService.AddToQueue("Increase");
                        break;
                    case "2":
                        ThreadService.AddToQueue("Decrease");
                        break;
                    case "3":
                        ThreadService.AddToQueue("Changed");
                        break;
                    case "4":
                        ThreadService.AddToQueue("Equals");
                        return;
                    default:
                        Console.WriteLine("Invalid choice");
                        break;
                }
                Console.WriteLine("Scanning...");
                Thread.Sleep(250);
                ThreadService.WaitUntilNotScanning();
            }
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
