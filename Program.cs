using GameMemoryWizard.Models;
using GameMemoryWizard.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;

namespace GameMemoryWizard {
    internal class Program
    {
        // Todo -> Handle Initial Scan
        // Todo -> Add a second cheat
        // Todo -> Checksum of app
        // Todo -> Play Audio File when you find it #3
        static void Main(string[] args)
        {
            try {
                Thread menuThread = new Thread(() => {
                    MenuService.DisplayMenu();
                });
                menuThread.Start();

                ThreadService.WaitForProcessName();

                List<ProcessMemory> previousScan = MemoryReadService.SearchAllMemoryOfProcess(ThreadService.RetrieveProcessName(), 80, 100);

                Thread keyboardShortcutThread = new Thread(() => {
                    KeyboardShortcutService.SetKeyboardShortcut();
                });
                keyboardShortcutThread.Start();

                Console.WriteLine("Ready!");
                ThreadService.SetHasFoundAddress(false);
                while (!ThreadService.RetrieveHasFoundAddress()) {
                    if (ThreadService.RetrieveQueueDepth() > 0) {
                        ThreadService.SetIsCurrentlyScanning(true);
                        string scanType = ThreadService.Dequeue();
                        var fitleredProcesses = MemoryReadService.FilterResults(previousScan, ThreadService.RetrieveProcessName(), scanType);
                        if (fitleredProcesses.Count == 1 && fitleredProcesses.First().CurrentCountOfMemoryLocations == 1) {
                            ThreadService.SetHasFoundAddress(true);

                            GameModel gameModel = JsonSerializer.Deserialize<GameModel>(ThreadService.RetrieveGameData());
                            CheatModel cheat = gameModel.RetrieveCheat(ThreadService.RetrieveCurrentCheat());
                            long offset = fitleredProcesses.First().CalculateOffsetForSingleMemoryLocation();
                            cheat.OffsetInMemory = offset;
                            cheat.RegionInfo = fitleredProcesses.First().RetrieveRegionInfo();
                            FileService.SerializeObjectToFile(gameModel, $"{gameModel.GameName}.json");
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
                KeyboardShortcutService.RemoveKeyboardShortcut();
                Console.WriteLine("KeyBoard Shorcut are no longer listening");
            }
        }
    }
}

// Potential Todos
// Thread filtering per thread file
// Disable/Enable cheat