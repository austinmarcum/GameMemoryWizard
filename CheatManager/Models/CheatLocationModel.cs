using System;

namespace CheatManager.Models {
    public class CheatLocationModel {
        public CheatModel Cheat { get; set; }
        public IntPtr MemoryLocationForCheat { get; set; }
        public bool DoesSignatureNeedRefined { get; set; }
        public ProcessMemory ProcessMemory;

        public CheatLocationModel(CheatModel cheat, ProcessMemory processMemory, IntPtr memoryLocation, bool doesSignatureNeedRefined) {
            Cheat = cheat;
            MemoryLocationForCheat = memoryLocation;
            DoesSignatureNeedRefined = doesSignatureNeedRefined;
            ProcessMemory = processMemory;
        }
    }
}
