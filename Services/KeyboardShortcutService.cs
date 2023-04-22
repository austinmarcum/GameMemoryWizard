﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace GameMemoryWizard {
    class KeyboardShortcutService {

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        public static void SetKeyboardShortcut() {
            _hookID = SetHook(_proc);
            Application.Run();
        }

        public static void RemoveKeyboardShortcut() {
            Application.Exit();
            UnhookWindowsHookEx(_hookID);
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr SetHook(LowLevelKeyboardProc proc) {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule) {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam) {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN) {
                int vkCode = Marshal.ReadInt32(lParam);
                if (Control.ModifierKeys == Keys.Shift && vkCode == (int)Keys.Add) {
                    ScanQueueService.AddToQueue("Increase");
                    return (IntPtr)1; 
                }
                if (Control.ModifierKeys == Keys.Shift && vkCode == (int)Keys.Subtract) {
                    ScanQueueService.AddToQueue("Decrease");
                    return (IntPtr)1; 
                }
                if (Control.ModifierKeys == Keys.Shift && vkCode == (int)Keys.E) {
                    ScanQueueService.AddToQueue("Equals");
                    return (IntPtr)1; 
                }
                if (Control.ModifierKeys == Keys.Shift && vkCode == (int)Keys.C) {
                    ScanQueueService.AddToQueue("Changed");
                    return (IntPtr)1; 
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
