using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameMemoryWizard.Services {

    public class FileService {

        private static string DEFAULT_FOLDER = "C:\\temp\\Memory\\";

        public static void SerializeObjectToFile(object obj, string fileName) {
            string jsonString = JsonSerializer.Serialize(obj);
            File.WriteAllText(DEFAULT_FOLDER + fileName, jsonString);
        }

        public static void StoreMemoryAsJson(Dictionary<IntPtr, int> memory, string fileName) {
            Dictionary<int, int> memoryLocationAsIntMemory = new Dictionary<int, int>();
            foreach (KeyValuePair<IntPtr, int> entry in memory) {
                int key = entry.Key.ToInt32();
                memoryLocationAsIntMemory[key] = entry.Value;
            }
            SerializeObjectToFile(memoryLocationAsIntMemory, fileName);
        }

        public static T DeserializeObjectFromFile<T>(string fileName) {
            string jsonString = File.ReadAllText(DEFAULT_FOLDER + fileName);
            T obj = JsonSerializer.Deserialize<T>(jsonString);
            return obj;
        }

        public static Dictionary<IntPtr, int> RetrieveMemoryFromJson(string fileName) {
            Dictionary<int, int> memoryLocationAsIntMemory = DeserializeObjectFromFile<Dictionary<int, int>>(fileName);
            Dictionary<IntPtr, int> memory = new Dictionary<IntPtr, int>();
            foreach (KeyValuePair<int, int> entry in memoryLocationAsIntMemory) {
                IntPtr key = new IntPtr(entry.Key);
                memory[key] = entry.Value;
            }
            return memory;
        }

        public static void DeleteFile(string fileName) {
            File.Delete(DEFAULT_FOLDER + fileName);
        }

        private static void EnsureDirectoryExists(string directory) {
            if (!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
