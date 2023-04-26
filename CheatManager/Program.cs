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

                Console.WriteLine("Ready!");
                ThreadService.SetHasFoundAddress(false);
                while (!ThreadService.RetrieveHasFoundAddress()) {
                    if (ThreadService.RetrieveQueueDepth() > 0) {
                        ThreadService.SetIsCurrentlyScanning(true);
                        string scanType = ThreadService.Dequeue();
                        var fitleredProcesses = MemoryService.FilterResults(previousScan, gameModel.ProcessName, scanType);
                        if (fitleredProcesses.Count == 1 && fitleredProcesses.First().CurrentCountOfMemoryLocations == 1) {
                            ThreadService.SetHasFoundAddress(true);
                            long offset = fitleredProcesses.First().CalculateOffsetForSingleMemoryLocation();
                            cheat.OffsetInMemory = offset;
                            cheat.RegionInfo = fitleredProcesses.First().RetrieveRegionInfo();
                            FileService.SerializeObjectToFile(gameModel, $"{gameModel.GameName}.json", FileService.GAME_FOLDER);
                            Console.WriteLine($"Offset: {offset}");
                        } else {
                            previousScan = fitleredProcesses;
                        }
                        ThreadService.SetIsCurrentlyScanning(false);
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
    }
}
