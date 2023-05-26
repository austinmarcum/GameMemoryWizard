namespace CheatManager.Models {
    public class MemoryRegionModel {
        public MEMORY_BASIC_INFORMATION Region { get; set; }
        public double ProbabilityOfCorrectRegion { get; set; }

        public MemoryRegionModel(MEMORY_BASIC_INFORMATION region, double probability) {
            Region = region;
            ProbabilityOfCorrectRegion = probability;
        }
    }
}
