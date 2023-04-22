using System.Collections.Concurrent;

namespace GameMemoryWizard {
    static class ScanQueueService {
        private static ConcurrentQueue<string> queue = new ConcurrentQueue<string>();

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
    }
}
