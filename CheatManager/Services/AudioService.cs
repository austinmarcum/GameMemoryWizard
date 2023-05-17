using System.Media;

namespace CheatManager.Services {
    public class AudioService {

        public static void PlayChime() {
            using (var soundPlayer = new SoundPlayer(@"c:\Windows\Media\chimes.wav")) {
                soundPlayer.Play(); 
            }
        }
    }
}
