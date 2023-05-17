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

        public static Dictionary<MEMORY_BASIC_INFORMATION, double> FindClosestMemoryRegionPerSignature(CheatModel cheat, List<ModuleMemoryInfo> moduleMemoryList, GameModel gameModel, Process gameProcess, Dictionary<string, MEMORY_BASIC_INFORMATION> foundMemoryRegions) {
            string regionId = cheat.RegionId;
            if (foundMemoryRegions.ContainsKey(regionId)) {
                Dictionary<MEMORY_BASIC_INFORMATION, double> result = new Dictionary<MEMORY_BASIC_INFORMATION, double>();
                result.Add(foundMemoryRegions[regionId], 100);
                return result;
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
            var topResults = sortedItems.Take(20);
            Dictionary<MEMORY_BASIC_INFORMATION, double> highestProbablyRegions = new Dictionary<MEMORY_BASIC_INFORMATION, double>();
            foreach (var item in topResults) {
                MEMORY_BASIC_INFORMATION region = item.Key;
                int numberOfMatches = item.Value;
                double probability = Math.Round((numberOfMatches / (double)totalNumberOfBytesInSignature) * 100.0, 2);
                highestProbablyRegions.Add(region, probability);
            }

            return highestProbablyRegions;
        }

        private static Dictionary<MEMORY_BASIC_INFORMATION, int> RetrieveNumberOfMatchesPerRegion(Dictionary<int, byte> regionSignatureChunk, int chunkIndex, ModuleMemoryInfo module, CheatModel cheat, IntPtr processHandle, Dictionary<MEMORY_BASIC_INFORMATION, int> numberOfMatchesPerRegion) {
            int chunkSize = 10 * 1024;
            List<MEMORY_BASIC_INFORMATION> regions = module.Regions.FindAll(x => x.RetrieveRegionInfo() == cheat.RegionInfo);
            foreach (MEMORY_BASIC_INFORMATION region in regions) {
                int numberOfMatchingBytes = 0;
                var bufferSize = Math.Min(region.RegionSize.ToInt32(), chunkSize);
                var buffer = new byte[bufferSize];
                IntPtr startAddress = region.BaseAddress + (chunkIndex * chunkSize);
                if (ReadProcessMemory(processHandle, startAddress, buffer, Convert.ToInt32(bufferSize), out var bytesRead)) {
                    for (var i = 0; i < bytesRead.ToInt64(); i++) {
                        int indexOfMemory = (chunkIndex * chunkSize) + i; // Might need a plus 1 here
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
    }
}
