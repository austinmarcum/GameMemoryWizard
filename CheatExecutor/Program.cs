using CheatManager.Models;
using CheatManager.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                while (IsProcessRunning(gameModel.ProcessName)) {
                    foreach (KeyValuePair<CheatModel, IntPtr> cheat in memoryLocationPerCheat) {
                        MemoryService.WriteMemory(cheat.Key.Amount, processHandle, cheat.Value);
                    }
                    Thread.Sleep(1000);
                }
            } catch (Exception e) {
                Console.WriteLine($"Error: {e.Message}");
                Thread.Sleep(5000);
            }
        }

        private static ProcessMemory FindRegionOfMemoryForCheat(List<ProcessMemory> memoryRegions, CheatModel cheat) {
            foreach (ProcessMemory region in memoryRegions) {
                if (region.RetrieveRegionInfo() == cheat.RegionInfo) {
                    return region;
                }
            }
            return null;
        }

        private static Dictionary<CheatModel, IntPtr> RetrieveMemoryLocationPerCheat(Process gameProcess, GameModel gameModel) {
            Dictionary<CheatModel, IntPtr> memoryLocationPerCheat = new Dictionary<CheatModel, IntPtr>();
            List<ProcessMemory> memoryRegions = MemoryService.RetrieveMemoryRegionsForProcess(gameProcess);
            foreach (CheatModel cheat in gameModel.Cheats) {
                ProcessMemory memoryRegion = FindRegionOfMemoryForCheat(memoryRegions, cheat);
                IntPtr locationInMemoryForCheat = IntPtr.Add(memoryRegion.BaseAddress, Convert.ToInt32(cheat.OffsetInMemory));
                memoryLocationPerCheat.Add(cheat, locationInMemoryForCheat);
            }
            return memoryLocationPerCheat;
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
    }
}
