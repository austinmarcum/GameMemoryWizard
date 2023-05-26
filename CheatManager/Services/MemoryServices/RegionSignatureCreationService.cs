using CheatManager.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace CheatManager.Services.MemoryServices {
    public class RegionSignatureCreationService {
        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesRead);

        public static List<string> ReadMemoryRegion(ProcessMemory regionOfMemory, GameModel gameModel, CheatModel cheat, List<string> previousScanFileNames = null, int number = 1) {
            FileService.EnsureGameRegionsFolderExists();
            var process = Process.GetProcessesByName(gameModel.ProcessName)[0];
            var processHandle = process.Handle;
            UIntPtr endAddress = UIntPtr.Zero;
            var chunkSize = 50 * 1024 * 1024; // 50MBs
            //var chunkSize = 10 * 1024; // 50MBs
            IntPtr startAddress = regionOfMemory.BaseAddress;
            endAddress = new UIntPtr(Convert.ToUInt64(startAddress.ToInt64() + regionOfMemory.RegionSize.ToInt64()));
            int regionChunkIndex = 1;
            List<string> fileNames = new List<string>();
            int totalBytesRead = 0;

            if (regionOfMemory.State.ToString() != AllocationProtectEnum.PAGE_GUARD.ToString()) {
                var remainingBytes = regionOfMemory.RegionSize.ToInt64();
                while (remainingBytes > 0 && IsProcessStillRunning(process)) {
                    string fileName = $"{gameModel.GameName}-{cheat.RegionId}-{regionChunkIndex}.json";
                    Dictionary<int, byte> previousScanOfChunk = RetrieveBytesForPreviousRegionScan(fileName, previousScanFileNames);
                    var bufferSize = Math.Min(remainingBytes, chunkSize);
                    var buffer = new byte[bufferSize];
                    if (ReadProcessMemory(processHandle, startAddress, buffer, Convert.ToInt32(bufferSize), out var bytesRead)) {
                        
                        Dictionary<int, byte> indexedBytes = new Dictionary<int, byte>();
                        for (var i = 0; i < bytesRead.ToInt64(); i++) {
                            byte value = buffer[i];
                           // if (!value.Equals(0)) {
                                if (previousScanOfChunk == null) {
                                    indexedBytes.Add(totalBytesRead, value);
                                } else if (previousScanOfChunk.ContainsKey(totalBytesRead) && previousScanOfChunk[totalBytesRead].Equals(value)) {
                                    indexedBytes.Add(totalBytesRead, value);
                                }
                                totalBytesRead++;
                            //}
                        }

                        if (IsProcessStillRunning(process)) {
                            FileService.SerializeObjectToFile(indexedBytes, fileName, FileService.GAME_REGIONS_FOLDER);
                            fileNames.Add(fileName);
                            regionChunkIndex++;
                        }
                    }
                    startAddress += Convert.ToInt32(bufferSize);
                    remainingBytes -= bufferSize;
                }
            }
            return fileNames;
        }

        private static Dictionary<int, byte> RetrieveBytesForPreviousRegionScan(string fileName, List<string> previousScanFileNames) {
            if (previousScanFileNames != null && previousScanFileNames.Contains(fileName) && FileService.DoesGameRegionExist(fileName)) {
                return FileService.DeserializeObjectFromFile<Dictionary<int, byte>>(fileName, FileService.GAME_REGIONS_FOLDER);
            }
            return null;
        }

        public static void FindRegionSignature(ProcessMemory regionOfMemory, GameModel gameModel, CheatModel cheat) {
            List<string> fileNames = ReadMemoryRegion(regionOfMemory, gameModel, cheat, null, -1);
            for (int i = 0; i < 10; i++) {
                Thread.Sleep(15 * 1000);
                Console.WriteLine("Scanning...");
                fileNames = ReadMemoryRegion(regionOfMemory, gameModel, cheat, fileNames, i);
            }
            //Console.WriteLine("Go to the main menu and press enter to conintue");
            //Console.ReadLine();
            //for (int i = 0; i < 3; i++) {
            //    Thread.Sleep(5 * 1000);
            //    Console.WriteLine("Scanning...");
            //    fileNames = ReadMemoryRegion(regionOfMemory, gameModel, cheat, fileNames, i);
           // }
            //Console.WriteLine("Finally re-enter the game and press enter to conintue");
            //Console.ReadLine();
            //for (int i = 0; i < 3; i++) {
             ///   Thread.Sleep(5 * 1000);
               // Console.WriteLine("Scanning...");
                //fileNames = ReadMemoryRegion(regionOfMemory, gameModel, cheat, fileNames, i);
           // }
            Console.WriteLine("Completed Region Signature.");
        }

        public static bool IsProcessStillRunning(Process process) {
            return !process.HasExited;
        }

        public static void RefineRegionSignature(CheatLocationModel cheatLocationModel, GameModel gameModel) {
            string regionId = cheatLocationModel.Cheat.RegionId;
            List<string> fileNames = FileService.RetrieveRegionFileNames($"{gameModel.GameName}-{cheatLocationModel.Cheat.RegionId}");
            while(true) {
                Thread.Sleep(15 * 1000);
                fileNames = ReadMemoryRegion(cheatLocationModel.ProcessMemory, gameModel, cheatLocationModel.Cheat, fileNames);
            }
        }

        public static string GenerateRegionSignature(string fileName) {
            Dictionary<int, byte> uniqueValuesPerRegion = FileService.DeserializeObjectFromFile<Dictionary<int, byte>>(fileName, FileService.GAME_REGIONS_FOLDER);
            int maxIndex = uniqueValuesPerRegion.Keys.Max();

            // Create a list of bytes with -1 values
            byte defaultByte = 0;
            List<byte> byteList = Enumerable.Repeat(defaultByte, maxIndex + 1).ToList();

            // Iterate over the dictionary and update the byte list at the specified indexes
            foreach (var kvp in uniqueValuesPerRegion) {
                int index = kvp.Key;
                byte value = kvp.Value;
                byteList[index] = value;
            }

            var a = SplitListByValue<List<List<byte>>>(byteList, defaultByte);
            return null;
        }

        private static List<List<byte>> SplitListByValue<T>(List<byte> list, byte value) {
            List<List<byte>> resultList = new List<List<byte>>();

            List<byte> subList = new List<byte>();
            foreach (byte item in list) {
                if (item.Equals(value)) {
                    if (subList.Count > 0) {
                        resultList.Add(subList);
                        subList = new List<byte>();
                    }
                } else {
                    subList.Add(item);
                }
            }

            if (subList.Count > 0) {
                resultList.Add(subList);
            }

            return resultList;
        }
    }
}
