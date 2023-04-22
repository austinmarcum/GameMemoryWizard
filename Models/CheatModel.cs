using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameMemoryWizard.Models {
    public enum CheatType {
       Lock,
       Multiplier,
       IncreaseTo,
       DecreaseTo
    }

    public class CheatModel {
        public string CheatName { get; set; }
        public bool IsEnabled { get; set; }
        public int OffsetInMemory { get; set; }
        public string RegionInfo { get; set; }
        public CheatType CheatType { get; set; }
        public int Amount { get; set; }
    }
}
