#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

namespace TumbangPreso
{
    // Use the Unity player window's own branded icons for both shell icon sizes.
    internal static class WindowsTaskbarIcon
    {
        private const uint SetIcon = 0x0080;
        private const uint AbortIfHung = 2;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Application.isEditor || Application.isBatchMode) return;
            if (!Assign()) Application.focusChanged += AssignOnFocus;
        }

        private static void AssignOnFocus(bool focused)
        {
            if (focused && Assign()) Application.focusChanged -= AssignOnFocus;
        }

        private static bool Assign()
        {
            using var process = Process.GetCurrentProcess();
            IntPtr window = process.MainWindowHandle;
            if (window == IntPtr.Zero) return false;
            SetFromClass(window, -14, 1); // Large icon.
            SetFromClass(window, -34, 0); // Small icon.
            return true;
        }

        private static void SetFromClass(IntPtr window, int classSlot, int size)
        {
            IntPtr icon = GetClassLongPtr(window, classSlot);
            if (icon != IntPtr.Zero)
                SendMessageTimeout(window, SetIcon, new IntPtr(size), icon, AbortIfHung, 100, out _);
        }

        [DllImport("user32.dll", EntryPoint = "GetClassLongPtrW")]
        private static extern IntPtr GetClassLongPtr(IntPtr window, int index);

        [DllImport("user32.dll", EntryPoint = "SendMessageTimeoutW")]
        private static extern IntPtr SendMessageTimeout(IntPtr window, uint message, IntPtr wParam,
            IntPtr lParam, uint flags, uint timeout, out IntPtr result);
    }
}
#endif
