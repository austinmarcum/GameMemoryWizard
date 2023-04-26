using CheatManager.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CheatManager.Services {
    public class MemoryService {

        public static List<ProcessMemory> SearchAllMemoryOfProcess(string processName, int minValue, int maxValue) {
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
            while (VirtualQueryEx(processHandle, endAddress, out memoryInfo, (uint)Marshal.SizeOf(memoryInfo)) > 0) {
                var memory = new Dictionary<IntPtr, int>();
                startAddress = memoryInfo.BaseAddress;
                try {
                    new UIntPtr(Convert.ToUInt64(startAddress.ToInt64() + memoryInfo.RegionSize.ToInt64()));
                } catch (Exception) {
                    return listOfProcessMemory;
                }

                endAddress = new UIntPtr(Convert.ToUInt64(startAddress.ToInt64() + memoryInfo.RegionSize.ToInt64()));

                if (memoryInfo.State.ToString() != AllocationProtectEnum.PAGE_GUARD.ToString()) {
                    var buffer = new byte[memoryInfo.RegionSize.ToInt64()];
                    if (ReadProcessMemory(processHandle, startAddress, buffer, buffer.Length, out var bytesRead)) {
                        for (var i = 0; i < bytesRead.ToInt64(); i += sizeof(int)) {
                            var address = startAddress + i;
                            var bufferValue = BitConverter.ToInt32(buffer, (int)i);
                            if (bufferValue >= minValue && bufferValue <= maxValue) {
                                memory[address] = bufferValue;
                            }
                        }
                    }
                    
                }

                if (memory.Count != 0) {
                    string fileName = $"Region-{regionNumber++}.json";
                    listOfProcessMemory.Add(new ProcessMemory(memoryInfo, memory, fileName));
                }
            }

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
            while (VirtualQueryEx(processHandle, endAddress, out memoryInfo, (uint)Marshal.SizeOf(memoryInfo)) > 0) {
                startAddress = memoryInfo.BaseAddress;
                try {
                    new UIntPtr(Convert.ToUInt64(startAddress.ToInt64() + memoryInfo.RegionSize.ToInt64()));
                } catch (Exception) {
                    return memoryRegions;
                }
                endAddress = new UIntPtr(Convert.ToUInt64(startAddress.ToInt64() + memoryInfo.RegionSize.ToInt64()));
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

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public AllocationProtectEnum AllocationProtect;
            public IntPtr RegionSize;
            public StateEnum State;
            public AllocationProtectEnum Protect;
            public TypeEnum Type;
        }

        public enum StateEnum : uint {
            MEM_COMMIT = 0x1000,
            MEM_FREE = 0x10000,
            MEM_RESERVE = 0x2000
        }

        public enum TypeEnum : uint {
            MEM_IMAGE = 0x1000000,
            MEM_MAPPED = 0x40000,
            MEM_PRIVATE = 0x20000
        }

        public enum AllocationProtectEnum : uint {
            PAGE_EXECUTE = 0x10,
            PAGE_EXECUTE_READ = 0x20,
            PAGE_EXECUTE_READWRITE = 0x40,
            PAGE_EXECUTE_WRITECOPY = 0x80,
            PAGE_NOACCESS = 0x01,
            PAGE_READONLY = 0x02,
            PAGE_READWRITE = 0x04,
            PAGE_WRITECOPY = 0x08,
            PAGE_GUARD = 0x100,
            PAGE_NOCACHE = 0x200,
            PAGE_WRITECOMBINE = 0x400
        }

        [DllImport("kernel32.dll")]
        private static extern int VirtualQueryEx(IntPtr hProcess, UIntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);
    }
}
