using System.Collections.Concurrent;
using System.Threading;

namespace GameMemoryWizard {
    static class ThreadService {
        private static ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
        private static bool isCurrentlyScanning;
        private static bool hasFoundAddress;
        private static string gameData;
        private static string currentCheat;

        public static void AddToQueue(string scanType) {
            queue.Enqueue(scanType);
        }

        public static int RetrieveQueueDepth() {
            return queue.Count;
        }

        public static string Dequeue() {
            string scanType;
            queue.TryDequeue(out scanType);
            return scanType;
        }

        public static string WaitForGameDataFromUi() {
            while (gameData == null) {
                Thread.Sleep(1000);
            }
            return gameData;
        }

        public static void SetIsCurrentlyScanning(bool isScanning) {
            isCurrentlyScanning = isScanning;
        }

        public static bool WaitUntilNotScanning() {
            while (isCurrentlyScanning) {
                Thread.Sleep(500);
            }
            return isCurrentlyScanning;
        }

        public static void SetHasFoundAddress(bool hasFoundMemoryAddress) {
            hasFoundAddress = hasFoundMemoryAddress;
        }

        public static bool RetrieveHasFoundAddress() {
            return hasFoundAddress;
        }

        public static void SetGameData(string uiGameData) {
            gameData = uiGameData;
        }

        public static string RetrieveGameData() {
            return gameData;
        }

        public static void SetCurrentCheat(string cheat) {
            currentCheat = cheat;
        }

        public static string RetrieveCurrentCheat() {
            return currentCheat;
        }
    }
}
