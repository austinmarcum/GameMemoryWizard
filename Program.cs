using GameMemoryWizard.Models;
using GameMemoryWizard.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GameMemoryWizard {
    internal class Program
    {   
        // Todo -> Save data to file per game #1
        static void Main(string[] args)
        {
            try {
                Thread menuThread = new Thread(() => {
                    MenuService.DisplayMenu();
                });
                menuThread.Start();

                // Todo -> Checksum of app
                ThreadService.WaitForProcessName();

                List<ProcessMemory> previousScan = MemoryReadService.SearchAllMemoryOfProcess(ThreadService.RetrieveProcessName(), 80, 100);

                Thread keyboardShortcutThread = new Thread(() => {
                    KeyboardShortcutService.SetKeyboardShortcut();
                });
                keyboardShortcutThread.Start();

                Console.WriteLine("Ready!");
                bool hasFoundAddress = false;
                while (!hasFoundAddress) {
                    if (ThreadService.RetrieveQueueDepth() > 0) {
                        ThreadService.SetIsCurrentlyScanning(true);
                        string scanType = ThreadService.Dequeue();
                        var fitleredProcesses = MemoryReadService.FilterResults(previousScan, ThreadService.RetrieveProcessName(), scanType);
                        if (fitleredProcesses.Count == 1 && fitleredProcesses.First().CurrentCountOfMemoryLocations == 1) {
                            hasFoundAddress = true;
                            // Todo -> Play Audio File when you find it #3
                            Console.WriteLine("FOUND IT!!!!!!!!!!");
                            long offset = fitleredProcesses.First().CalculateOffsetForSingleMemoryLocation();
                            Console.WriteLine($"Offset: {offset}");
                        } else {
                            previousScan = fitleredProcesses;
                        }
                        ThreadService.SetIsCurrentlyScanning(false);
                    }
                    Thread.Sleep(250);
                }
            } catch (Exception e) {
                Console.WriteLine($"Error: {e.Message}");
                Thread.Sleep(5000);
            } finally {
                KeyboardShortcutService.RemoveKeyboardShortcut();
                Console.WriteLine("KeyBoard Shorcut are no longer listening");
            }
        }
    }
}

// Potential Todos
// Thread filtering per thread file