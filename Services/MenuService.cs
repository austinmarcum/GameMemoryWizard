using GameMemoryWizard.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;

namespace GameMemoryWizard.Services {
    public class MenuService {

        public static string RetrieveResponse(string command) {
            Console.WriteLine("\r\n" + command);
            return Console.ReadLine();
        }

        public static int RetrieveNumberResponse(string command, int defaultNumber) {
            int retryCount = 0;
            var maxRetries = 20;

            while (retryCount < maxRetries) {
                try {
                    if (command != "") {
                        Console.WriteLine("\r\n" + command);
                    }
                    string response = Console.ReadLine();
                    return Convert.ToInt32(response);
                } catch (Exception) {
                    retryCount++;
                    Console.WriteLine("Response Must be a number, please try again\r\n");
                }
            }
            Console.WriteLine($"Response could not be determined. Defaulting to {defaultNumber}.\r\n");
            return defaultNumber;
        }

        public static void DisplayMenu() {
            string welcomeMessage = "Welcome to the Game Memory Wizard (Reader).\r\n";
            Console.WriteLine(welcomeMessage);
            string gameName = RetrieveGame();
            //string processName = RetrieveProcessName();
            string processName = "BasicConsole";
            ThreadService.SetProcressName(processName);
            string cheatName = RetrieveResponse("What would you like the first cheat to be called?");
            ThreadService.SetCurrentCheat(cheatName);
            CheatType cheatType = RetrieveCheatType();
            int cheatAmount = RetrieveNumberResponse("What is the amount for the cheat?", 0);
            GameModel gameModel = new GameModel(gameName, processName, new CheatModel(cheatName, cheatType, cheatAmount));
            ThreadService.SetGameData(JsonSerializer.Serialize(gameModel));
            HandleScanning();
        }

        public static void HandleScanning() {
            Console.WriteLine("\r\n");
            Console.WriteLine("To find the memory location, change the value in the game and perform the cooresponding scan.");
            while (!ThreadService.RetrieveHasFoundAddress()) {
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
                        break;
                    default:
                        Console.WriteLine("Invalid choice");
                        break;
                }
                Console.WriteLine("Scanning...");
                Thread.Sleep(250);
                ThreadService.WaitUntilNotScanning();
            }
        }

        public static CheatType RetrieveCheatType() {
            Console.WriteLine("\r\n");
            Console.WriteLine("What is the cheat type?");
            Console.WriteLine("1. Lock");
            Console.WriteLine("2. Multiplier");
            Console.WriteLine("3. Increase To");
            Console.WriteLine("4. Decrease To");
            string reponse = Console.ReadLine();
            if (reponse == "1") { return CheatType.Lock; }
            if (reponse == "2") { return CheatType.Multiplier; }
            if (reponse == "3") { return CheatType.IncreaseTo; }
            if (reponse == "4") { return CheatType.DecreaseTo; }
            Console.WriteLine("Unknown Choice... Defaulting to Lock");
            return CheatType.Lock;
        }

        public static string RetrieveGame() {
            Console.WriteLine("\r\n");
            Console.WriteLine("What game would you like to cheat in?");
            List<string> games = FileService.RetrieveGameList();
            int index = 1;
            foreach (string game in games) {
                Console.WriteLine($"{index++}. {game}");
            }
            Console.WriteLine($"{index}. New Game");
            int gameIndex = RetrieveNumberResponse("", 1);
            if (gameIndex == index) {
                return RetrieveResponse("Please enter the name of the game:");
            }
            return games[gameIndex -1];
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
