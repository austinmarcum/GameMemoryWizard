using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;

namespace CheatManager.Services {
    public class AudioService {

        public static void PlayChime() {
            using (var soundPlayer = new SoundPlayer(@"c:\Windows\Media\chimes.wav")) {
                soundPlayer.Play(); 
            }
        }
    }
}
