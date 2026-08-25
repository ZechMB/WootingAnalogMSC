using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AnalogKeyboardMSC
{
    internal static class Statics
    {
        public static bool IsWootingSDKActive = false;
        public static bool AreKeysLocked = false;
        public static bool ShouldDisableInputsWhenTabbedOut = true;
        public static ushort ZForwardKey = 0;
        public static ushort ZReverseKey = 0;
        public static ushort ZLeftKey = 0;
        public static ushort ZRightKey = 0;
        public static ushort ZClutchKey = 0;
        public static ushort ZHandbrakeKey = 0;
        public static ushort ZPlayerLeftKey = 0;
        public static ushort ZPlayerRightKey = 0;
        public static ushort ZPlayerUpKey = 0;
        public static ushort ZPlayerDownKey = 0;
        public static float ZForward = 0;
        public static float ZReverse = 0;
        public static float ZLeft = 0;
        public static float ZRight = 0;
        public static float ZClutch = 0;
        public static float ZHandbrake = 0;
        public static float ZPlayerLeft = 0;
        public static float ZPlayerRight = 0;
        public static float ZPlayerUp = 0;
        public static float ZPlayerDown = 0;
    }

    //ai coded
    public static class WindowCheck
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public static bool IsGameFocused()
        {
            IntPtr activatedHandle = GetForegroundWindow();
            if (activatedHandle == IntPtr.Zero) return false;

            GetWindowThreadProcessId(activatedHandle, out uint activeProcId);

            // Compares the active window's Process ID with the current game Process ID
            return activeProcId == Process.GetCurrentProcess().Id;
        }
    }
}
