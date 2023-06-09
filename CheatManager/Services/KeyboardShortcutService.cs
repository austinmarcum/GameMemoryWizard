﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace CheatManager.Services {
    public class KeyboardShortcutService {

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _procForManager = HookCallbackForManager;
        private static LowLevelKeyboardProc _procForExecutor = HookCallbackForExecutor;
        private static IntPtr _hookID = IntPtr.Zero;

        public static void SetKeyboardShortcutForManager() {
            _hookID = SetHook(_procForManager);
            Application.Run();
        }

        public static void RemoveKeyboardShortcutForManager() {
            Application.Exit();
            UnhookWindowsHookEx(_hookID);
        }

        public static void SetKeyboardShortcutForExecutor() {
            _hookID = SetHook(_procForExecutor);
            Application.Run();
        }

        public static void RemoveKeyboardShortcutForExecutor() {
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

        private static IntPtr HookCallbackForManager(int nCode, IntPtr wParam, IntPtr lParam) {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN) {
                int vkCode = Marshal.ReadInt32(lParam);
                if (Control.ModifierKeys == Keys.Shift && vkCode == (int)Keys.Add) {
                    ThreadService.AddToQueue("Increase");
                    return (IntPtr)1; 
                }
                if (Control.ModifierKeys == Keys.Shift && vkCode == (int)Keys.Subtract) {
                    ThreadService.AddToQueue("Decrease");
                    return (IntPtr)1; 
                }
                if (Control.ModifierKeys == Keys.Shift && vkCode == (int)Keys.E) {
                    ThreadService.AddToQueue("Equals");
                    return (IntPtr)1; 
                }
                if (Control.ModifierKeys == Keys.Shift && vkCode == (int)Keys.C) {
                    ThreadService.AddToQueue("Changed");
                    return (IntPtr)1; 
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private static IntPtr HookCallbackForExecutor(int nCode, IntPtr wParam, IntPtr lParam) {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN) {
                int vkCode = Marshal.ReadInt32(lParam);
                if (Control.ModifierKeys == Keys.Shift && vkCode == (int)Keys.Add) {
                    ThreadService.SetUserRequestedCheat("Increase");
                    return (IntPtr)1;
                }
                if (Control.ModifierKeys == Keys.Shift && vkCode == (int)Keys.Subtract) {
                    ThreadService.SetUserRequestedCheat("Decrease");
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
