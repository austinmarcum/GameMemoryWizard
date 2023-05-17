using CheatManager.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace CheatManager.Services.MemoryServices {
    public class MemoryService {

        public static List<ProcessMemory> SearchAllMemoryOfProcess(string processName, int minValue, int maxValue) {
            var timer = new Stopwatch();
            timer.Start();
            int regionNumber = 0;
            List<ProcessMemory> listOfProcessMemory = new List<ProcessMemory>();
            var process = Process.GetProcessesByName(processName)[0];
            var processHandle = process.Handle;
            if (processHandle == IntPtr.Zero) {
                return listOfProcessMemory;
            }

            var startAddress = IntPtr.Zero;
            UIntPtr endAddress = UIntPtr.Zero;
            var memoryInfo = new MEMORY_BASIC_INFORMATION();
            var chunkSize = 50 * 1024 * 1024; // 50MBs
            while (VirtualQueryEx(processHandle, endAddress, out memoryInfo, (uint)Marshal.SizeOf(memoryInfo)) > 0) {
                var memory = new Dictionary<IntPtr, int>();
                startAddress = memoryInfo.BaseAddress;
                try {
                    new UIntPtr(Convert.ToUInt64(startAddress.ToInt64() + memoryInfo.RegionSize.ToInt64()));
                } catch (Exception) {
                    Console.WriteLine("Total Time for Scan: " + timer.ElapsedMilliseconds);
                    return listOfProcessMemory;
                }

                endAddress = new UIntPtr(Convert.ToUInt64(startAddress.ToInt64() + memoryInfo.RegionSize.ToInt64()));

                if (memoryInfo.State.ToString() != AllocationProtectEnum.PAGE_GUARD.ToString()) {
                    var remainingBytes = memoryInfo.RegionSize.ToInt64();
                    while (remainingBytes > 0) {
                        var bufferSize = Math.Min(remainingBytes, chunkSize);
                        var buffer = new byte[bufferSize];
                        if (ReadProcessMemory(processHandle, startAddress, buffer, Convert.ToInt32(bufferSize), out var bytesRead)) {
                            for (var i = 0; i < bytesRead.ToInt64(); i += sizeof(int)) {
                                var address = startAddress + i;
                                var bufferValue = BitConverter.ToInt32(buffer, (int)i);
                                if (bufferValue >= minValue && bufferValue <= maxValue) {
                                    memory[address] = bufferValue;
                                }
                            }
                        }
                        startAddress += Convert.ToInt32(bufferSize);
                        remainingBytes -= bufferSize;
                    }
                }

                if (memory.Count != 0) {
                    string fileName = $"Region-{regionNumber++}.json";
                    listOfProcessMemory.Add(new ProcessMemory(memoryInfo, memory, fileName));
                }
            }

            Console.WriteLine("Total Time for Scan: "+ timer.ElapsedMilliseconds);
            return listOfProcessMemory;
        }

        public static List<ProcessMemory> FilterResults(List<ProcessMemory> previousScan, string processName, string filterName) {
            List<ProcessMemory> currentScan = new List<ProcessMemory>();
            var process = Process.GetProcessesByName(processName)[0];
            foreach (ProcessMemory memoryOfProcess in previousScan) {
                ProcessMemory filteredProcessMemory = new ProcessMemory(memoryOfProcess);
                Dictionary<IntPtr, int> filteredResults = new Dictionary<IntPtr, int>();
                foreach (KeyValuePair<IntPtr, int> memory in memoryOfProcess.RetrieveMemory()) {
                    IntPtr address = memory.Key;
                    int previousValue = memory.Value;
                    int value = 0;

                    byte[] buffer = new byte[sizeof(int)];

                    if (ReadProcessMemory(process.Handle, address, buffer, buffer.Length, out var bytesRead)) {
                        value = BitConverter.ToInt32(buffer, 0);
                        if (filterName == "Equals") {
                            if (value == previousValue) {
                                filteredResults[address] = value;
                            }
                        }
                        if (filterName == "Changed") {
                            if (value != previousValue) {
                                filteredResults[address] = value;
                            }
                        }
                        if (filterName == "Increase") {
                            if (value > previousValue) {
                                filteredResults[address] = value;
                            }
                        }
                        if (filterName == "Decrease") {
                            if (value < previousValue) {
                                filteredResults[address] = value;
                            }
                        }
                    }
                }
                if (filteredResults.Count > 0) {
                    filteredProcessMemory.SetProcessMemory(filteredResults);
                    currentScan.Add(filteredProcessMemory);
                } else {
                    filteredProcessMemory.RemoveFile();
                }
                
            }
            Console.WriteLine(RetrieveFilterScanResults(previousScan, currentScan));
            return currentScan;
        }

        public static List<ProcessMemory> RetrieveMemoryRegionsForProcess(Process process) {
            List<ProcessMemory> memoryRegions = new List<ProcessMemory>();
            IntPtr processHandle = process.Handle;
            if (processHandle == IntPtr.Zero) {
                return memoryRegions;
            }

            var startAddress = IntPtr.Zero;
            UIntPtr endAddress = UIntPtr.Zero;
            var memoryInfo = new MEMORY_BASIC_INFORMATION();
            List<string> viewedRegions = new List<string>();
            while (VirtualQueryEx(processHandle, endAddress, out memoryInfo, (uint)Marshal.SizeOf(memoryInfo)) > 0) {
                viewedRegions.Add(memoryInfo.RetrieveRegionInfo());
                startAddress = memoryInfo.BaseAddress;
                try {
                    new UIntPtr(Convert.ToUInt64(startAddress.ToInt64() + memoryInfo.RegionSize.ToInt64()));
                } catch (Exception) {
                    return memoryRegions;
                }
                endAddress = new UIntPtr(Convert.ToUInt64(startAddress.ToInt64() + memoryInfo.RegionSize.ToInt64()));
                int regionIndex = viewedRegions.FindAll(x => x == memoryInfo.RetrieveRegionInfo()).Count;
                memoryRegions.Add(new ProcessMemory(memoryInfo));
            }
            return memoryRegions;
        }

        public static void WriteMemory(int value, IntPtr processHandle, IntPtr address) {
            byte[] buffer = BitConverter.GetBytes(value);
            IntPtr bytesWritten;
            WriteProcessMemory(processHandle, address, buffer, buffer.Length, out bytesWritten);
        }

        public static int ReadMemory(IntPtr processHandle, IntPtr address) {
            byte[] buffer = new byte[sizeof(int)];
            ReadProcessMemory(processHandle, address, buffer, buffer.Length, out var bytesRead);
            int value = BitConverter.ToInt32(buffer, 0);
            return value;
        }

        private static string RetrieveFilterScanResults(List<ProcessMemory> previousScan, List<ProcessMemory> currentScan) {
            int totalNumberOfFilteredMemoryLocationsInPreviousScan = 0;
            int totalNumberOfFilteredMemoryLocationsInCurrentScan = 0;
            foreach (ProcessMemory memoryOfProcess in previousScan) {
                totalNumberOfFilteredMemoryLocationsInPreviousScan += memoryOfProcess.CurrentCountOfMemoryLocations;
            }
            foreach (ProcessMemory memoryOfProcess in currentScan) {
                totalNumberOfFilteredMemoryLocationsInCurrentScan += memoryOfProcess.CurrentCountOfMemoryLocations;
            }

            int numberOfFilteredOutLocations = totalNumberOfFilteredMemoryLocationsInPreviousScan - totalNumberOfFilteredMemoryLocationsInCurrentScan;
            return $"{totalNumberOfFilteredMemoryLocationsInCurrentScan} Memory Locations Remaining ({numberOfFilteredOutLocations} filtered out)";
        }

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

        [DllImport("kernel32.dll")]
        private static extern int VirtualQueryEx(IntPtr hProcess, UIntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);

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

        public const uint PROCESS_QUERY_INFORMATION = 0x0400;
        public const uint PROCESS_VM_READ = 0x0010;
        public const int MAX_PATH = 260;
    }
}
