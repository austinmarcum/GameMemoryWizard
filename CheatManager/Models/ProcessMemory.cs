using CheatManager.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CheatManager.Models {
    public class ProcessMemory {
        public IntPtr BaseAddress { get; set; }
        public IntPtr AllocationBase { get; set; }
        public AllocationProtectEnum AllocationProtect { get; set; }
        public IntPtr RegionSize { get; set; }
        public StateEnum State { get; set; }
        public AllocationProtectEnum Protect { get; set; }
        public TypeEnum Type { get; set; }

        public string FileLocationOfMemory { get; set; }
        public bool DoesMemoryFileExist { get; set; }
        public int CurrentCountOfMemoryLocations { get; set; }

        public ProcessMemory(MEMORY_BASIC_INFORMATION memoryStruct, Dictionary<IntPtr, int> memoryOfProcess, string fileName) {
            BaseAddress = memoryStruct.BaseAddress;
            AllocationBase = memoryStruct.AllocationBase;
            AllocationProtect = memoryStruct.AllocationProtect;
            RegionSize = memoryStruct.RegionSize;
            State = memoryStruct.State;
            Protect = memoryStruct.Protect;
            Type = memoryStruct.Type;
            FileLocationOfMemory = fileName;
            FileService.StoreMemoryAsJson(memoryOfProcess, fileName);
            DoesMemoryFileExist = true;
            CurrentCountOfMemoryLocations = memoryOfProcess.Count;
        }

        public ProcessMemory(ProcessMemory previousProcessMemory) {
            BaseAddress = previousProcessMemory.BaseAddress;
            AllocationBase = previousProcessMemory.AllocationBase;
            AllocationProtect = previousProcessMemory.AllocationProtect;
            RegionSize = previousProcessMemory.RegionSize;
            State = previousProcessMemory.State;
            Protect = previousProcessMemory.Protect;
            Type = previousProcessMemory.Type;
            FileLocationOfMemory = previousProcessMemory.FileLocationOfMemory;
        }

        public ProcessMemory(MEMORY_BASIC_INFORMATION memoryStruct) {
            BaseAddress = memoryStruct.BaseAddress;
            AllocationBase = memoryStruct.AllocationBase;
            AllocationProtect = memoryStruct.AllocationProtect;
            RegionSize = memoryStruct.RegionSize;
            State = memoryStruct.State;
            Protect = memoryStruct.Protect;
            Type = memoryStruct.Type;
            FileLocationOfMemory = null;
            DoesMemoryFileExist = false;
            CurrentCountOfMemoryLocations = 0;
        }

        public void SetProcessMemory(Dictionary<IntPtr, int> memoryOfProcess) {
            FileService.StoreMemoryAsJson(memoryOfProcess, FileLocationOfMemory);
            DoesMemoryFileExist = true;
            CurrentCountOfMemoryLocations = memoryOfProcess.Count;
        }

        public long CalculateOffsetForSingleMemoryLocation() {
            Dictionary<IntPtr, int> memory = RetrieveMemory();
            if (memory.Count != 1) {
                throw new ApplicationException("Calcuating Offset should only be for a single memory location");
            }
            Console.WriteLine($"{memory.First().Key} -> {memory.First().Value}");
            IntPtr locationOfMemory = memory.Take(1).Select(d => d.Key).First();
            return IntPtr.Subtract(locationOfMemory, BaseAddress.ToInt32()).ToInt64();
        }

        public List<long> CalculateOffsetForMultipleMemoryLocations() {
            List<long> offsets = new List<long>();
            Dictionary<IntPtr, int> memory = RetrieveMemory();
            foreach (KeyValuePair<IntPtr, int> entry in memory) {
                Console.WriteLine($"{entry.Key} -> {entry.Value}");
                IntPtr locationOfMemory = entry.Key;
                offsets.Add(IntPtr.Subtract(locationOfMemory, BaseAddress.ToInt32()).ToInt64());
            }
            return offsets;
        }

        public Dictionary<IntPtr, int> RetrieveMemory() {
            if (!DoesMemoryFileExist) {
                return new Dictionary<IntPtr, int>();
            }
            return FileService.RetrieveMemoryFromJson(FileLocationOfMemory);
        }

        public void RemoveFile() {
            FileService.DeleteFile(FileLocationOfMemory, FileService.DEFAULT_MEMORY_FOLDER);
            DoesMemoryFileExist = false;
            CurrentCountOfMemoryLocations = 0;
        }

        public string RetrieveRegionInfo() {
            return $"{AllocationProtect.ToString()}-{RegionSize}-{State}-{Protect}-{Type}";
        }
    }
}
