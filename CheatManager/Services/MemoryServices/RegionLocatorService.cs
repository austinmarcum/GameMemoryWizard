using CheatManager.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace CheatManager.Services.MemoryServices {
    public class RegionLocatorService {

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesRead);


        public static MemoryRegionModel FindClosestMemoryRegionPerSignature(CheatModel cheat, List<ModuleMemoryInfo> moduleMemoryList, GameModel gameModel, Process gameProcess, Dictionary<string, MemoryRegionModel> foundMemoryRegions) {
            string regionId = cheat.RegionId;
            if (foundMemoryRegions.ContainsKey(regionId)) {
                return foundMemoryRegions[regionId];
            }
            ModuleMemoryInfo module = moduleMemoryList.Find(x => x.ModuleName == cheat.ModuleName);
            List<string> regionFileNames = FileService.RetrieveRegionFileNames($"{gameModel.GameName}-{cheat.RegionId}");
            Dictionary<MEMORY_BASIC_INFORMATION, int> numberOfMatchesPerRegion = new Dictionary<MEMORY_BASIC_INFORMATION, int>();
            int totalNumberOfBytesInSignature = 0;
            int chunkIndex = 0;
            Console.WriteLine($"Scanning for Memory Region...");
            foreach (string fileName in regionFileNames) {
                Dictionary<int, byte> regionSignatureChunk = FileService.DeserializeObjectFromFile<Dictionary<int, byte>>(fileName, FileService.GAME_REGIONS_FOLDER);
                RetrieveNumberOfMatchesPerRegion(regionSignatureChunk, chunkIndex, module, cheat, gameProcess.Handle, numberOfMatchesPerRegion);
                chunkIndex++;
                totalNumberOfBytesInSignature += regionSignatureChunk.Count;

                double percentComplete = Math.Round((chunkIndex / (double)regionFileNames.Count) * 100, 2);
                Console.WriteLine($"Scanning for Memory Region is {percentComplete}% complete");
            }

            var sortedItems = numberOfMatchesPerRegion.OrderByDescending(kv => kv.Value);
            //var topResults = sortedItems.Take(30);
            Dictionary<MEMORY_BASIC_INFORMATION, double> highestProbablyRegions = new Dictionary<MEMORY_BASIC_INFORMATION, double>();
            foreach (var item in sortedItems) {
                MEMORY_BASIC_INFORMATION region = item.Key;
                int numberOfMatches = item.Value;
                double probability = Math.Round((numberOfMatches / (double)totalNumberOfBytesInSignature) * 100.0, 2);
                highestProbablyRegions.Add(region, probability);
            }

            return RetrieveMemoryRegionForCheat(highestProbablyRegions, cheat, gameProcess.Handle);
        }

        private static Dictionary<MEMORY_BASIC_INFORMATION, int> RetrieveNumberOfMatchesPerRegion(Dictionary<int, byte> regionSignatureChunk, int chunkIndex, ModuleMemoryInfo module, CheatModel cheat, IntPtr processHandle, Dictionary<MEMORY_BASIC_INFORMATION, int> numberOfMatchesPerRegion) {
            int chunkSize = 50 * 1024 * 1024;
            List<MEMORY_BASIC_INFORMATION> regions = module.Regions.FindAll(x => x.RetrieveRegionInfo() == cheat.RegionInfo);
            foreach (MEMORY_BASIC_INFORMATION region in regions) {
                int numberOfMatchingBytes = 0;
                var bufferSize = Math.Min(region.RegionSize.ToInt32(), chunkSize);
                var buffer = new byte[bufferSize];
                IntPtr startAddress = region.BaseAddress + (chunkIndex * chunkSize);
                if (ReadProcessMemory(processHandle, startAddress, buffer, Convert.ToInt32(bufferSize), out var bytesRead)) {
                    for (var i = 0; i < bytesRead.ToInt64(); i++) {
                        int indexOfMemory = (chunkIndex * chunkSize) + i;
                        if (regionSignatureChunk.ContainsKey(indexOfMemory) && regionSignatureChunk[indexOfMemory] == buffer[i]) {
                            numberOfMatchingBytes++;
                        }
                    }
                }


                if (numberOfMatchesPerRegion.ContainsKey(region)) {
                    numberOfMatchesPerRegion[region] = numberOfMatchesPerRegion[region] + numberOfMatchingBytes;
                } else {
                    numberOfMatchesPerRegion.Add(region, numberOfMatchingBytes);
                }
            }
            return numberOfMatchesPerRegion;
        }

        private static MemoryRegionModel RetrieveMemoryRegionForCheat(Dictionary<MEMORY_BASIC_INFORMATION, double> highestProbablyRegions, CheatModel cheat, IntPtr processHandle) {
            if (highestProbablyRegions.Count == 1) {
                var region = highestProbablyRegions.First().Key;
                return new MemoryRegionModel(region, highestProbablyRegions[region]);
            }
            if (cheat.RangeForCheat.First() == cheat.RangeForCheat.Last()) {
                // The user knew the value at the time when the cheat was created, therefore they will be able to confirm the value again
                Dictionary<MEMORY_BASIC_INFORMATION, string> valueOfMemoryPerRegion = new Dictionary<MEMORY_BASIC_INFORMATION, string>();
                foreach (KeyValuePair<MEMORY_BASIC_INFORMATION, double> entry in highestProbablyRegions) {
                    MEMORY_BASIC_INFORMATION region = entry.Key;
                    IntPtr locationInMemoryForCheat = IntPtr.Add(region.BaseAddress, Convert.ToInt32(cheat.OffsetInMemory));
                    int valueInMemory = MemoryService.ReadMemory(processHandle, locationInMemoryForCheat);
                    valueOfMemoryPerRegion.Add(region, valueInMemory.ToString());
                }
                Console.WriteLine($"Because the value of {cheat.CheatName} isn't known. Please enter the value.");
                string response = Console.ReadLine();
                foreach (KeyValuePair<MEMORY_BASIC_INFORMATION, string> entry in valueOfMemoryPerRegion) {
                    if (response == entry.Value) {
                        return new MemoryRegionModel(entry.Key, 100);
                    }
                }

                var regionOfCheat = MenuService.SelectFromDictionary(valueOfMemoryPerRegion, $"There were multiple memory regions found. Please select the value of {cheat.CheatName} that the game is currently set to:");
                return new MemoryRegionModel(regionOfCheat, highestProbablyRegions[regionOfCheat]);
            }

            Console.WriteLine($"There were multiple memory regions found. Because the value of {cheat.CheatName} isn't known. The location of memory will be changed in each region");
            Console.WriteLine($"When it is confirmed that the cheat has been activated. Please select y.");
            foreach (KeyValuePair<MEMORY_BASIC_INFORMATION, double> entry in highestProbablyRegions) {
                MEMORY_BASIC_INFORMATION region = entry.Key;
                IntPtr locationInMemoryForCheat = IntPtr.Add(region.BaseAddress, Convert.ToInt32(cheat.OffsetInMemory));
                int previousValue = MemoryService.ReadMemory(processHandle, locationInMemoryForCheat);
                MemoryService.WriteMemory(cheat.Amount, processHandle, locationInMemoryForCheat);
                Console.WriteLine($"\r\nNext Region has been changed (Previous Value of {previousValue}) has been changed. Has the value in the game been changed to {cheat.Amount} (Y/N)?");
                string response = Console.ReadLine();
                if (response.ToLower() == "y") {
                    return new MemoryRegionModel(region, entry.Value);
                }
                MemoryService.WriteMemory(previousValue, processHandle, locationInMemoryForCheat);
            }
            throw new ApplicationException("Unable to find Region");
        }
    }
}
