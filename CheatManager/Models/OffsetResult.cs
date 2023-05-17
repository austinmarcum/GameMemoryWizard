using System;
using System.Collections.Generic;

namespace CheatManager.Models {
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
}
