namespace GameMemoryWizard.Models {
    public class GameModel {
        public string GameName { get; set; }
        public string ProcessName { get; set; }
        public string GameChecksum { get; set; }

        public CheatModel[] Cheats { get; set; }
    }
}
