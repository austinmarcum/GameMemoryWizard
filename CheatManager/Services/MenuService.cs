using CheatManager.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;

namespace CheatManager.Services {
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
            string processName = RetrieveProcessName();
            string cheatName = RetrieveResponse("What would you like the cheat to be called?");
            ThreadService.SetCurrentCheat(cheatName);
            CheatType cheatType = RetrieveCheatType();
            int cheatAmount = RetrieveNumberResponse("What is the amount for the cheat?", 0);
            int[] rangeOfStartingScan = RetrieveRangeOfInitialScan();
            CheatModel cheatModel = new CheatModel(cheatName, cheatType, cheatAmount, rangeOfStartingScan);
            if (FileService.DoesGameExist(gameName)) {
                GameModel gameModel = FileService.DeserializeObjectFromFile<GameModel>(gameName + ".json", FileService.GAME_FOLDER);
                gameModel.Cheats.Add(cheatModel);
                ThreadService.SetGameData(JsonSerializer.Serialize(gameModel));
            } else {
                GameModel gameModel = new GameModel(gameName, processName, cheatModel);
                ThreadService.SetGameData(JsonSerializer.Serialize(gameModel));
            }
           
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
                if (ThreadService.RetrieveHasPossiblyFoundAddress()) {
                    Console.WriteLine("5. Add Memory Locations as cheats");
                }
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
                    case "5":
                        ThreadService.AddToQueue("SaveAllLocationsAsCheats");
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

        private static int[] RetrieveRangeOfInitialScan() {
            bool doesKnowValue = RetrieveResponse("Do you know the current value that you are trying to change (Y/N)?").ToLower() == "y";
            if (doesKnowValue) {
                int value = RetrieveNumberResponse("Enter the current value:", 0);
                return new int[] { value, value };
            }
            int lowestNumber = RetrieveNumberResponse("What is the lowest value of this variable:", -100);
            int highestNumber = RetrieveNumberResponse("What is the highest value of this variable:", 1000);
            return new int[] { lowestNumber, highestNumber };
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

        public static T SelectFromDictionary<T>(Dictionary<T, string> dictionary, string message) {
            int retryCount = 0;
            var maxRetries = 5;

            while (retryCount < maxRetries) {
                Console.WriteLine(message);
                int index = 0;
                Dictionary<string, T> indexPerChoice = new Dictionary<string, T>();
                foreach (KeyValuePair<T, string> entry in dictionary) {
                    Console.WriteLine($"{index++}. {entry.Value}");
                    indexPerChoice.Add(index.ToString(), entry.Key);
                }
                string response = Console.ReadLine();
                if (indexPerChoice.ContainsKey(response)) {
                    return indexPerChoice[response];
                } else {
                    Console.WriteLine("Invalid choice");
                    retryCount++;
                }
            }
            throw new ApplicationException("Unable to find option");
        }
    }
}
