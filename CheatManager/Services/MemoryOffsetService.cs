using CheatManager.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static CheatManager.Services.MemoryService;

namespace CheatManager.Services {

    public class ModuleMemoryInfo {
        public string ModuleName { get; set; }
        public IntPtr StartingLocation { get; set; }
        public List<MEMORY_BASIC_INFORMATION> Regions { get; set; }

        public ModuleMemoryInfo(string moduleName) {
            ModuleName = moduleName;
            Regions = new List<MEMORY_BASIC_INFORMATION>();
        }
    }

    public class OffsetResult {
        public List<long> OffsetsForModule { get; set; }
        public string ModuleName { get; set; }
        public string RegionId { get; set; }

        public OffsetResult(string moduleName, List<long> offsets) {
            ModuleName = moduleName;
            OffsetsForModule = offsets;
            RegionId = Guid.NewGuid().ToString().Substring(0, 10);
        }
    }

    public class MemoryOffsetService {

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);


        [DllImport("psapi.dll")]
        public static extern bool EnumProcessModules(IntPtr hProcess, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.U4)] [In][Out] IntPtr[] lphModule, uint cb, out uint lpcbNeeded);

        [DllImport("psapi.dll")]
        public static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] char[] lpBaseName, uint nSize);

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesRead);

        public const uint PROCESS_QUERY_INFORMATION = 0x0400;
        public const uint PROCESS_VM_READ = 0x0010;
        public const int MAX_PATH = 260;

        public static List<ModuleMemoryInfo> RetrieveModuleMemoryStructure(string processName) {
            var process = Process.GetProcessesByName(processName)[0];
            IntPtr hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, process.Id);
            List<ModuleMemoryInfo> modules = new List<ModuleMemoryInfo>();

            if (hProcess == IntPtr.Zero) {
                return modules;
            }

            IntPtr[] moduleHandles = new IntPtr[1024];
            uint cbNeeded;

            if (EnumProcessModules(hProcess, moduleHandles, (uint)(moduleHandles.Length * IntPtr.Size), out cbNeeded)) {
                uint moduleCount = cbNeeded / (uint)IntPtr.Size;
                for (uint i = 0; i < moduleCount; i++) {
                    char[] moduleName = new char[MAX_PATH];
                    if (GetModuleFileNameEx(hProcess, moduleHandles[i], moduleName, MAX_PATH) != 0) {
                        string moduleNameStr = new string(moduleName);
                        moduleNameStr = moduleNameStr.Substring(0, moduleNameStr.IndexOf('\0'));

                        IntPtr moduleBaseAddress = moduleHandles[i];
                        ModuleMemoryInfo moduleMemory = new ModuleMemoryInfo(moduleNameStr);
                        IntPtr currentAddress = moduleBaseAddress;
                        MEMORY_BASIC_INFORMATION memoryInfo;
                        while (VirtualQueryEx(hProcess, currentAddress, out memoryInfo, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION))) != 0) {
                            if (memoryInfo.State == StateEnum.MEM_COMMIT && memoryInfo.Protect != AllocationProtectEnum.PAGE_NOACCESS) // Check for committed memory and exclude PAGE_NOACCESS regions
                            {
                                moduleMemory.Regions.Add(memoryInfo);
                            }

                            currentAddress = IntPtr.Add(memoryInfo.BaseAddress, (int)memoryInfo.RegionSize);
                        }

                        modules.Add(moduleMemory);
                    }
                }
            }

            CloseHandle(hProcess);
            return modules;
        }

        public static OffsetResult RetrieveOffsetForMemory(ProcessMemory regionOfMemory, string processName) {
            List<long> offsetsPerRegion = regionOfMemory.CalculateOffsetForMultipleMemoryLocations();
            List<ModuleMemoryInfo> moduleMemoryList = RetrieveModuleMemoryStructure(processName);
            ModuleMemoryInfo moduleContainingMemoryInQuestion = moduleMemoryList.Where(x => x.Regions.Where(region => region.BaseAddress == regionOfMemory.BaseAddress) != null).First();
            return new OffsetResult(moduleContainingMemoryInQuestion.ModuleName, offsetsPerRegion);
        }

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

        public static Dictionary<CheatModel, IntPtr> RetrieveMemoryLocationPerCheat(Process gameProcess, GameModel gameModel) {
            Dictionary<CheatModel, IntPtr> memoryLocationPerCheat = new Dictionary<CheatModel, IntPtr>();
            List<ModuleMemoryInfo> moduleMemoryList = RetrieveModuleMemoryStructure(gameProcess.ProcessName);
            Dictionary<string, MEMORY_BASIC_INFORMATION> foundMemoryRegions = new Dictionary<string, MEMORY_BASIC_INFORMATION>();
            foreach (CheatModel cheat in gameModel.Cheats) {
                MEMORY_BASIC_INFORMATION memoryRegionForCheat = FindMemoryRegionForCheatBySignature(cheat, moduleMemoryList, gameModel, gameProcess, foundMemoryRegions);
                if (!foundMemoryRegions.ContainsKey(cheat.RegionId)) {
                    foundMemoryRegions.Add(cheat.RegionId, memoryRegionForCheat);
                }
                IntPtr locationInMemoryForCheat = IntPtr.Add(memoryRegionForCheat.BaseAddress, Convert.ToInt32(cheat.OffsetInMemory));
                memoryLocationPerCheat.Add(cheat, locationInMemoryForCheat);
            }
            return memoryLocationPerCheat;
        }

        private static MEMORY_BASIC_INFORMATION FindMemoryRegionForCheatBySignature(CheatModel cheat, List<ModuleMemoryInfo> moduleMemoryList, GameModel gameModel, Process gameProcess, Dictionary<string, MEMORY_BASIC_INFORMATION> foundMemoryRegions) {
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
            MEMORY_BASIC_INFORMATION memoryRegionForCheat = new MEMORY_BASIC_INFORMATION();
            int highestNumberOfMatches = 0;
            foreach (KeyValuePair<MEMORY_BASIC_INFORMATION, int> entry in numberOfMatchesPerRegion) {
                if (entry.Value > highestNumberOfMatches) {
                    highestNumberOfMatches = entry.Value;
                    memoryRegionForCheat = entry.Key;
                }
            }

            double probability = Math.Round((highestNumberOfMatches / (double)totalNumberOfBytesInSignature) * 100.0, 2);
            Console.WriteLine($"Found Region with {probability}% Probability");
            return memoryRegionForCheat;
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
