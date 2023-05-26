using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace CheatManager.Services {

    public class FileService {

        public static readonly string GAME_FOLDER = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "\\Games\\";
        public static readonly string GAME_REGIONS_FOLDER = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "\\GamesRegions\\";
        public static readonly string DEFAULT_MEMORY_FOLDER = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "\\TempMemoryStorage\\";

        public static void SerializeObjectToFile(object obj, string fileName, string folderName) {
            EnsureDirectoryExists(folderName);
            string jsonString = JsonSerializer.Serialize(obj);
            string filePath = folderName + fileName;
            if (File.Exists(filePath)) {
                File.Move(filePath, filePath + ".old");
            }
            File.WriteAllText(folderName + fileName, jsonString);
            if (File.Exists(filePath + ".old")) {
                File.Delete(filePath + ".old");
            }
        }
        public static void RestoreDotOldFiles() {
            RestoreDotOldFilesPerFolder(GAME_FOLDER);
            RestoreDotOldFilesPerFolder(GAME_REGIONS_FOLDER);
        }

        private static void RestoreDotOldFilesPerFolder(string folder) {
            DirectoryInfo gameDirectory = new DirectoryInfo(folder);
            FileInfo[] files = gameDirectory.GetFiles("*.old");
            foreach (FileInfo file in files) {
                string originalFileName = file.Name.Replace(".old", "");
                if (File.Exists(originalFileName)) {
                    File.Delete(originalFileName);
                }
                File.Move(file.Name, originalFileName);
            }
        }

        public static void StoreMemoryAsJson(Dictionary<IntPtr, int> memory, string fileName) {
            Dictionary<int, int> memoryLocationAsIntMemory = new Dictionary<int, int>();
            foreach (KeyValuePair<IntPtr, int> entry in memory) {
                int key = entry.Key.ToInt32();
                memoryLocationAsIntMemory[key] = entry.Value;
            }
            SerializeObjectToFile(memoryLocationAsIntMemory, fileName, DEFAULT_MEMORY_FOLDER);
        }

        public static T DeserializeObjectFromFile<T>(string fileName, string folderName) {
            EnsureDirectoryExists(folderName);
            string jsonString = File.ReadAllText(folderName + fileName);
            T obj = JsonSerializer.Deserialize<T>(jsonString);
            return obj;
        }

        public static Dictionary<IntPtr, int> RetrieveMemoryFromJson(string fileName) {
            Dictionary<int, int> memoryLocationAsIntMemory = DeserializeObjectFromFile<Dictionary<int, int>>(fileName, DEFAULT_MEMORY_FOLDER);
            Dictionary<IntPtr, int> memory = new Dictionary<IntPtr, int>();
            foreach (KeyValuePair<int, int> entry in memoryLocationAsIntMemory) {
                IntPtr key = new IntPtr(entry.Key);
                memory[key] = entry.Value;
            }
            return memory;
        }

        public static void DeleteFile(string fileName, string folderName) {
            File.Delete(folderName + fileName);
        }

        private static void EnsureDirectoryExists(string directory) {
            if (!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }
        }

        public static List<string> RetrieveGameList() {
            EnsureDirectoryExists(GAME_FOLDER);
            List<string> gameList = new List<string>();
            DirectoryInfo gameDirectory = new DirectoryInfo(GAME_FOLDER); 
            FileInfo[] files = gameDirectory.GetFiles("*.json");
            foreach (FileInfo file in files) {
                gameList.Add(file.Name.Replace(".json", ""));
            }
            return gameList;
        }

        private static bool DoesFileExist(string fileName, string folderName) {
            return File.Exists(folderName + fileName);
        }

        public static bool DoesGameExist(string gameName) {
            return DoesFileExist(gameName + ".json", GAME_FOLDER);
        }

        public static bool DoesGameRegionExist(string fileName) {
            return DoesFileExist(fileName, GAME_REGIONS_FOLDER);
        }

        public static void EnsureGameRegionsFolderExists() {
            EnsureDirectoryExists(GAME_REGIONS_FOLDER);
        }

        public static List<string> RetrieveRegionFileNames(string fileStartsWith) {
            DirectoryInfo directoryInfo = new DirectoryInfo(GAME_REGIONS_FOLDER);
            FileInfo[] files = directoryInfo.GetFiles(fileStartsWith + "*.json");
            List<string> fileNames = new List<string>();
            foreach (FileInfo fileInfo in files) {
                fileNames.Add(fileInfo.Name);
            }
            return fileNames;
        }
    }
}
