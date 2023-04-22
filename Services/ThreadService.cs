using System.Collections.Concurrent;
using System.Threading;

namespace GameMemoryWizard {
    static class ThreadService {
        private static ConcurrentQueue<string> queue = new ConcurrentQueue<string>();
        private static string processName;
        private static bool isCurrentlyScanning;

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

        public static void SetProcressName(string gameProcessName) {
            processName = gameProcessName;
        }

        public static string RetrieveProcessName() {
            return processName;
        }

        public static string WaitForProcessName() {
            while (processName == null) {
                Thread.Sleep(1000);
            }
            return processName;
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
    }
}
