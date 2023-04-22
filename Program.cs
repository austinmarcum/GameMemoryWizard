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
                Thread keyboardShortcutThread = new Thread(() => {
                    KeyboardShortcutService.SetKeyboardShortcut();
                    
                });
                keyboardShortcutThread.Start();

                Thread menuThread = new Thread(() => {
                    MenuService.DisplayWelcome();
                });
                menuThread.Start();

                // Todo -> Checksum of app

                List<ProcessMemory> previousScan = MemoryReadService.SearchAllMemoryOfProcess("BasicConsole", 80, 100);
                Console.WriteLine("Ready!");
                bool hasFoundAddress = false;
                while (!hasFoundAddress) {
                    if (ScanQueueService.RetrieveQueueDepth() > 0) {
                        string scanType = ScanQueueService.Dequeue();
                        var fitleredProcesses = MemoryReadService.FilterResults(previousScan, "BasicConsole", scanType);
                        if (fitleredProcesses.Count == 1 && fitleredProcesses.First().CurrentCountOfMemoryLocations == 1) {
                            hasFoundAddress = true;
                            // Todo -> Play Audio File when you find it #3
                            Console.WriteLine("FOUND IT!!!!!!!!!!");
                            long offset = fitleredProcesses.First().CalculateOffsetForSingleMemoryLocation();
                            Console.WriteLine($"Offset: {offset}");
                        } else {
                            previousScan = fitleredProcesses;
                        }
                    }
                    Thread.Sleep(250);
                }

                menuThread.Join();
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