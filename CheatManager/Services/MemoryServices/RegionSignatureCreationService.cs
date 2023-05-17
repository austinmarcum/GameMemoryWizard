using CheatManager.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace CheatManager.Services.MemoryServices {
    public class RegionSignatureCreationService {
        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesRead);

        private static List<string> ReadMemoryRegion(ProcessMemory regionOfMemory, GameModel gameModel, CheatModel cheat, List<string> previousScanFileNames = null) {
            FileService.EnsureGameRegionsFolderExists();
            var process = Process.GetProcessesByName(gameModel.ProcessName)[0];
            var processHandle = process.Handle;
            UIntPtr endAddress = UIntPtr.Zero;
            //var chunkSize = 50 * 1024 * 1024; // 50MBs
            var chunkSize = 10 * 1024; // 50MBs
            IntPtr startAddress = regionOfMemory.BaseAddress;
            endAddress = new UIntPtr(Convert.ToUInt64(startAddress.ToInt64() + regionOfMemory.RegionSize.ToInt64()));
            int regionChunkIndex = 1;
            List<string> fileNames = new List<string>();
            int totalBytesRead = 0;

            if (regionOfMemory.State.ToString() != AllocationProtectEnum.PAGE_GUARD.ToString()) {
                var remainingBytes = regionOfMemory.RegionSize.ToInt64();
                while (remainingBytes > 0) {
                    string fileName = $"{gameModel.GameName}-{cheat.RegionId}-{regionChunkIndex}.json";
                    Dictionary<int, byte> previousScanOfChunk = RetrieveBytesForPreviousRegionScan(fileName, previousScanFileNames);
                    var bufferSize = Math.Min(remainingBytes, chunkSize);
                    var buffer = new byte[bufferSize];
                    if (ReadProcessMemory(processHandle, startAddress, buffer, Convert.ToInt32(bufferSize), out var bytesRead)) {
                        Dictionary<int, byte> indexedBytes = new Dictionary<int, byte>();
                        for (var i = 0; i < bytesRead.ToInt64(); i++) {
                            if (previousScanOfChunk == null) {
                                indexedBytes.Add(totalBytesRead, buffer[i]);
                            } else if (previousScanOfChunk.ContainsKey(totalBytesRead) && previousScanOfChunk[totalBytesRead].Equals(buffer[i])) {
                                indexedBytes.Add(totalBytesRead, buffer[i]);
                            }
                            totalBytesRead++;
                        }
                        FileService.SerializeObjectToFile(indexedBytes, fileName, FileService.GAME_REGIONS_FOLDER);
                        fileNames.Add(fileName);
                        regionChunkIndex++;
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
            List<string> fileNames = ReadMemoryRegion(regionOfMemory, gameModel, cheat);
            for (int i = 0; i < 10; i++) {
                Thread.Sleep(30 * 1000);
                fileNames = ReadMemoryRegion(regionOfMemory, gameModel, cheat, fileNames);
            }
        }
    }
}
