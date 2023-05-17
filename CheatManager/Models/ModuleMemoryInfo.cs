using System;
using System.Collections.Generic;

namespace CheatManager.Models {
    public class ModuleMemoryInfo {
        public string ModuleName { get; set; }
        public IntPtr StartingLocation { get; set; }
        public List<MEMORY_BASIC_INFORMATION> Regions { get; set; }

        public ModuleMemoryInfo(string moduleName) {
            ModuleName = moduleName;
            Regions = new List<MEMORY_BASIC_INFORMATION>();
        }
    }
}
