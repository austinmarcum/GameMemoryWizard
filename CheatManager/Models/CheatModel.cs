namespace CheatManager.Models {
    public enum CheatType {
       Lock,
       Multiplier,
       IncreaseTo,
       DecreaseTo
    }

    public enum MultiplierType {
        FixedRange,
        SporadicValues
    }

    public class CheatModel {
        public string CheatName { get; set; }
        public string ModuleName { get; set; }
        public bool IsEnabled { get; set; }
        public long OffsetInMemory { get; set; }
        public string RegionInfo { get; set; }
        public CheatType CheatType { get; set; }
        public MultiplierType MultiplierType { get; set; }
        public int Amount { get; set; }
        public int[] RangeForCheat { get; set; }
        public string RegionId { get; set; }

        // Used During Cheat Execution
        public int UnmodifiedValue { get; set; }
        public int ModifiedValue { get; set; }
        public int FirstModifiedValue { get; set; }

        public CheatModel(string cheatName, CheatType cheatType, int amount, int[] rangeForCheat) {
            CheatName = cheatName;
            CheatType = cheatType;
            Amount = amount;
            IsEnabled = true;
            RangeForCheat = rangeForCheat;
        }

        public CheatModel() { }

        public CheatModel(CheatModel otherCheatModel) {
            CheatName = otherCheatModel.CheatName;
            IsEnabled = otherCheatModel.IsEnabled;
            CheatType = otherCheatModel.CheatType;
            MultiplierType = otherCheatModel.MultiplierType;
            Amount = otherCheatModel.Amount;
            RangeForCheat = otherCheatModel.RangeForCheat;
            ModuleName = otherCheatModel.ModuleName;
        }
    }
}
