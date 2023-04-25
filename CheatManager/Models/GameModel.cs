using System.Collections.Generic;

namespace CheatManager.Models {
    public class GameModel {
        public string GameName { get; set; }
        public string ProcessName { get; set; }
        public string GameChecksum { get; set; } // Still to do

        public List<CheatModel> Cheats { get; set; }

        public GameModel(string gameName, string processName, CheatModel cheat) {
            GameName = gameName;
            ProcessName = processName;
            Cheats = new List<CheatModel> { cheat };
        }

        public GameModel() { }

        public CheatModel RetrieveCheat(string cheatName) {
            foreach (CheatModel cheat in Cheats) {
                if (cheat.CheatName == cheatName) {
                    return cheat;
                }
            }
            return null;
        }
    }
}
