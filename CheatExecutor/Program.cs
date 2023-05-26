using CheatManager.Models;
using CheatManager.Services;
using CheatManager.Services.MemoryServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace CheatExecutor {
    class Program {
        static void Main(string[] args) {
            List<Thread> regionSignatureThreads = new List<Thread>();
            try {
                if (args.Length == 0) {
                    throw new ApplicationException("No game name provided. Cannot start cheats");
                }
                FileService.RestoreDotOldFiles();
                GameModel gameModel = FileService.DeserializeObjectFromFile<GameModel>(args[0] + ".json", FileService.GAME_FOLDER);
                Process gameProcess = WaitForProcess(gameModel.ProcessName);
                IntPtr processHandle = gameProcess.Handle;
                List<CheatLocationModel> memoryLocationPerCheat = RetrieveMemoryLocationPerCheat(gameProcess, gameModel);
                regionSignatureThreads = SpinUpRegionSignatureThreads(memoryLocationPerCheat, gameModel);


                Thread keyboardShortcutThread = new Thread(() => {
                    KeyboardShortcutService.SetKeyboardShortcutForExecutor();
                });
                keyboardShortcutThread.Start();

                while (IsProcessRunning(gameModel.ProcessName)) {
                    foreach (CheatLocationModel cheatLocation in memoryLocationPerCheat) {
                        CheatModel cheat = cheatLocation.Cheat;
                        IntPtr locationInMemory = cheatLocation.MemoryLocationForCheat;
                        if (cheat.CheatType == CheatType.Lock) {
                            int currentValue = MemoryService.ReadMemory(processHandle, locationInMemory);
                            Console.WriteLine("Value of Memory: " + currentValue);
                            MemoryService.WriteMemory(cheat.Amount, processHandle, locationInMemory);
                        }
                        if (cheat.CheatType == CheatType.Multiplier) {
                            HandleMultipierCheat(cheat, locationInMemory, processHandle);
                        }
                    }
                    string userRequestedCheat = ThreadService.RetrieveUserRequestedCheat();
                    bool wasUserRequested = false;
                    if (userRequestedCheat == "Increase" || userRequestedCheat == "Decrease") {
                        foreach (CheatLocationModel cheatLocation in memoryLocationPerCheat) {
                            CheatModel cheat = cheatLocation.Cheat;
                            IntPtr locationInMemory = cheatLocation.MemoryLocationForCheat;
                            if (cheat.CheatType == CheatType.IncreaseTo && userRequestedCheat == "Increase") {
                                wasUserRequested = true;
                                int currentValue = MemoryService.ReadMemory(processHandle, locationInMemory);
                                int increasedValue = currentValue + cheat.Amount;
                                MemoryService.WriteMemory(increasedValue, processHandle, locationInMemory);
                            }
                            if (cheat.CheatType == CheatType.DecreaseTo && userRequestedCheat == "Decrease") {
                                wasUserRequested = true;
                                int currentValue = MemoryService.ReadMemory(processHandle, locationInMemory);
                                int increasedValue = currentValue - cheat.Amount;
                                MemoryService.WriteMemory(increasedValue, processHandle, locationInMemory);
                            }
                        }
                        ThreadService.SetUserRequestedCheat(null);
                    }
                    if (!wasUserRequested) {
                        Thread.Sleep(1000);
                    }
                }
            } catch (Exception e) {
                Console.WriteLine($"Error: {e.Message}");
                Thread.Sleep(5000);
            } finally {
                KeyboardShortcutService.RemoveKeyboardShortcutForExecutor();
                Console.WriteLine("KeyBoard Shorcut are no longer listening");
                EndAllRegionSignatureThreads(regionSignatureThreads);
            }
        }

        private static void HandleMultipierCheat(CheatModel cheat, IntPtr locationInMemory, IntPtr processHandle) {
            int currentValue = MemoryService.ReadMemory(processHandle, locationInMemory);
            if (CanBeMultiplied(currentValue, cheat)) {
                cheat.UnmodifiedValue = currentValue;
                cheat.ModifiedValue = currentValue * cheat.Amount;
                if (cheat.FirstModifiedValue == 0) {
                    cheat.FirstModifiedValue = cheat.ModifiedValue;
                }
                MemoryService.WriteMemory(cheat.ModifiedValue, processHandle, locationInMemory);
            }
        }

        private static bool CanBeMultiplied(int currentValue, CheatModel cheat) {
            if (cheat.UnmodifiedValue == 0) { return true; }
            if (cheat.MultiplierType == MultiplierType.FixedRange && (currentValue * cheat.Amount) < cheat.FirstModifiedValue) {
                return true;
            }
            if (cheat.MultiplierType == MultiplierType.SporadicValues && currentValue != cheat.ModifiedValue && currentValue != cheat.UnmodifiedValue) {
                return true;
            }
            return false;
        }

        private static Process WaitForProcess(string processName) {
            while (true) {
                Process[] processes = Process.GetProcessesByName(processName);
                if (processes.Length != 0) {
                    return processes[0];
                }
                Thread.Sleep(1000);
            }
        }

        private static bool IsProcessRunning(string processName) {
            Process[] processes = Process.GetProcessesByName(processName);
            return processes.Length != 0;
        }

        public static List<CheatLocationModel> RetrieveMemoryLocationPerCheat(Process gameProcess, GameModel gameModel) {
            List<CheatLocationModel> memoryLocationPerCheat = new List<CheatLocationModel>();
            List<ModuleMemoryInfo> moduleMemoryList = MemoryService.RetrieveModuleMemoryStructure(gameProcess.ProcessName);
            Dictionary<string, MemoryRegionModel> foundMemoryRegions = new Dictionary<string, MemoryRegionModel>();
            foreach (CheatModel cheat in gameModel.Cheats) {
                MemoryRegionModel memoryRegionForCheat = RegionLocatorService.FindClosestMemoryRegionPerSignature(cheat, moduleMemoryList, gameModel, gameProcess, foundMemoryRegions);
                if (!foundMemoryRegions.ContainsKey(cheat.RegionId)) {
                    foundMemoryRegions.Add(cheat.RegionId, memoryRegionForCheat);
                }
                IntPtr locationInMemoryForCheat = IntPtr.Add(memoryRegionForCheat.Region.BaseAddress, Convert.ToInt32(cheat.OffsetInMemory));
                memoryLocationPerCheat.Add(new CheatLocationModel(cheat, new ProcessMemory(memoryRegionForCheat.Region), locationInMemoryForCheat, memoryRegionForCheat.ProbabilityOfCorrectRegion != 100));
            }
            return memoryLocationPerCheat;
        }

        private static List<Thread> SpinUpRegionSignatureThreads(List<CheatLocationModel> cheatLocations, GameModel gameModel) {
            List<string> regionsInThread = new List<string>();
            List<Thread> regionSignatureRefinementThreads = new List<Thread>();
            List<CheatLocationModel> regionsToRefine = cheatLocations.Where(x => x.DoesSignatureNeedRefined).ToList();
            foreach (CheatLocationModel cheatLocation in regionsToRefine) {
                Thread regionSignatureRefinementThread = new Thread(() => {
                    RegionSignatureCreationService.RefineRegionSignature(cheatLocation, gameModel);
                });
                regionSignatureRefinementThreads.Add(regionSignatureRefinementThread);
                regionSignatureRefinementThread.Start();
            }
            return regionSignatureRefinementThreads;
        }

        private static void EndAllRegionSignatureThreads(List<Thread> threads) {
            foreach (Thread thread in threads) {
                thread.Abort();
            }
        }
    }
}
