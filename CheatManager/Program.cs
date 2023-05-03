using CheatManager.Models;
using CheatManager.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;


namespace CheatManager {
    class Program {
        // Todo -> Checksum of app
        // Todo -> Play Audio File when you find it
        // Todo -> Read Line in UI thread is still waiting even after cheat is saved
        // Todo -> Test Cheat before saving (will need writer portion first)
        static void Main(string[] args) {
            try {
                Thread menuThread = new Thread(() => {
                    MenuService.DisplayMenu();
                });
                menuThread.Start();

                ThreadService.WaitForGameDataFromUi();

                GameModel gameModel = JsonSerializer.Deserialize<GameModel>(ThreadService.RetrieveGameData());
                CheatModel cheat = gameModel.RetrieveCheat(ThreadService.RetrieveCurrentCheat());
                List<ProcessMemory> previousScan = MemoryService.SearchAllMemoryOfProcess(gameModel.ProcessName, cheat.RangeForCheat[0], cheat.RangeForCheat[1]);

                Thread keyboardShortcutThread = new Thread(() => {
                    KeyboardShortcutService.SetKeyboardShortcutForManager();
                });
                keyboardShortcutThread.Start();

                Console.WriteLine("Initial Scan Complete");
                ThreadService.SetHasFoundAddress(false);
                while (!ThreadService.RetrieveHasFoundAddress()) {
                    if (ThreadService.RetrieveQueueDepth() > 0) {
                        previousScan = FilterMemoryAndSaveCheatIfFound(cheat, gameModel, previousScan);
                    }
                    Thread.Sleep(250);
                }
            } catch (Exception e) {
                Console.WriteLine($"Error: {e.Message}");
                Thread.Sleep(5000);
            } finally {
                KeyboardShortcutService.RemoveKeyboardShortcutForManager();
                Console.WriteLine("KeyBoard Shorcut are no longer listening");
            }
        }

        private static void HandleFindingMemoryLocation(ProcessMemory regionOfMemory, CheatModel cheat, GameModel gameModel) {
            ThreadService.SetHasFoundAddress(true);
            long offset = regionOfMemory.CalculateOffsetForSingleMemoryLocation();
            cheat.OffsetInMemory = offset;
            cheat.RegionInfo = regionOfMemory.RetrieveRegionInfo();
            FileService.SerializeObjectToFile(gameModel, $"{gameModel.GameName}.json", FileService.GAME_FOLDER);
            Console.WriteLine($"Offset: {offset}");
        }

        private static List<ProcessMemory> FilterMemoryAndSaveCheatIfFound(CheatModel cheat, GameModel gameModel, List<ProcessMemory> previousScan) {
            ThreadService.SetIsCurrentlyScanning(true);
            string action = ThreadService.Dequeue();
            if (action == "SaveAllLocationsAsCheats") {
                HandleSavingMultipleCheats(cheat, gameModel, previousScan);
            }
            List<ProcessMemory> fitleredProcesses = MemoryService.FilterResults(previousScan, gameModel.ProcessName, action);

            if (fitleredProcesses.Count == 1 && fitleredProcesses.First().CurrentCountOfMemoryLocations == 1) {
                HandleFindingMemoryLocation(fitleredProcesses.First(), cheat, gameModel);
            } else if (CouldHaveFoundMultipleMatches(previousScan)) {
                ThreadService.SetHasPossiblyFoundAddress(true);
                Console.WriteLine("Real Value and Display Value might have been found. If you are unable to narrow it down to a single memory location. Then select option #5.");
            }
            ThreadService.SetIsCurrentlyScanning(false);
            return fitleredProcesses;
        }

        private static bool CouldHaveFoundMultipleMatches(List<ProcessMemory> previousScan) {
            if (previousScan.Count > 3) {
                return false; // More than 3 regions of memory still left to scan
            }
            int totalNumberOfMemoryAddressesLeft = 0;
            foreach (ProcessMemory region in previousScan) {
                totalNumberOfMemoryAddressesLeft += region.CurrentCountOfMemoryLocations;
            }
            return totalNumberOfMemoryAddressesLeft <= 3;
        }

        private static void HandleSavingMultipleCheats(CheatModel cheat, GameModel gameModel, List<ProcessMemory> previousScan) {
            gameModel.Cheats.Remove(cheat);
            Dictionary<ProcessMemory, long> offsetPerProcessMemory = new Dictionary<ProcessMemory, long>();
            foreach (ProcessMemory region in previousScan) {
                List<long> offsets = region.CalculateOffsetForMultipleMemoryLocations();
                foreach (long offset in offsets) {
                    offsetPerProcessMemory.Add(region, offset);
                }
            }
            foreach (KeyValuePair<ProcessMemory, long> entry in offsetPerProcessMemory) {
                CheatModel newCheat = new CheatModel(cheat);
                newCheat.OffsetInMemory = entry.Value;
                newCheat.RegionInfo = entry.Key.RetrieveRegionInfo();
                gameModel.Cheats.Add(newCheat);
            }
            FileService.SerializeObjectToFile(gameModel, $"{gameModel.GameName}.json", FileService.GAME_FOLDER);
            ThreadService.SetHasFoundAddress(true);
        }
    }
}
