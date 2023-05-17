﻿using CheatManager.Models;
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
            try {
                if (args.Length == 0) {
                    throw new ApplicationException("No game name provided. Cannot start cheats");
                }
                GameModel gameModel = FileService.DeserializeObjectFromFile<GameModel>(args[0] + ".json", FileService.GAME_FOLDER);
                Process gameProcess = WaitForProcess(gameModel.ProcessName);
                IntPtr processHandle = gameProcess.Handle;
                Dictionary<CheatModel, IntPtr> memoryLocationPerCheat = RetrieveMemoryLocationPerCheat(gameProcess, gameModel);

                Thread keyboardShortcutThread = new Thread(() => {
                    KeyboardShortcutService.SetKeyboardShortcutForExecutor();
                });
                keyboardShortcutThread.Start();

                while (IsProcessRunning(gameModel.ProcessName)) {
                    foreach (KeyValuePair<CheatModel, IntPtr> cheatEntry in memoryLocationPerCheat) {
                        CheatModel cheat = cheatEntry.Key;
                        IntPtr locationInMemory = cheatEntry.Value;
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
                        foreach (KeyValuePair<CheatModel, IntPtr> cheatEntry in memoryLocationPerCheat) {
                            CheatModel cheat = cheatEntry.Key;
                            IntPtr locationInMemory = cheatEntry.Value;
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

        public static Dictionary<CheatModel, IntPtr> RetrieveMemoryLocationPerCheat(Process gameProcess, GameModel gameModel) {
            Dictionary<CheatModel, IntPtr> memoryLocationPerCheat = new Dictionary<CheatModel, IntPtr>();
            List<ModuleMemoryInfo> moduleMemoryList = MemoryService.RetrieveModuleMemoryStructure(gameProcess.ProcessName);
            Dictionary<string, MEMORY_BASIC_INFORMATION> foundMemoryRegions = new Dictionary<string, MEMORY_BASIC_INFORMATION>();
            foreach (CheatModel cheat in gameModel.Cheats) {
                Dictionary<MEMORY_BASIC_INFORMATION, double> highestProbablyRegions = RegionLocatorService.FindClosestMemoryRegionPerSignature(cheat, moduleMemoryList, gameModel, gameProcess, foundMemoryRegions);
                MEMORY_BASIC_INFORMATION memoryRegionForCheat = RetrieveMemoryRegionForCheat(highestProbablyRegions, cheat);
                if (!foundMemoryRegions.ContainsKey(cheat.RegionId)) {
                    foundMemoryRegions.Add(cheat.RegionId, memoryRegionForCheat);
                }
                IntPtr locationInMemoryForCheat = IntPtr.Add(memoryRegionForCheat.BaseAddress, Convert.ToInt32(cheat.OffsetInMemory));
                memoryLocationPerCheat.Add(cheat, locationInMemoryForCheat);
            }
            return memoryLocationPerCheat;
        }

        private static MEMORY_BASIC_INFORMATION RetrieveMemoryRegionForCheat(Dictionary<MEMORY_BASIC_INFORMATION, double> highestProbablyRegions, CheatModel cheat) {
            if (highestProbablyRegions.Count == 1) {
                return highestProbablyRegions.First().Key;
            }
            if (cheat.RangeForCheat.First() == cheat.RangeForCheat.Last()) {
                // The user knew the value at the time when the cheat was created, therefore they will be able to confirm the value again

            }
            return new MEMORY_BASIC_INFORMATION();
        }
    }
}
