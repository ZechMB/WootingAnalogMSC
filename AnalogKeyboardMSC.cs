using Harmony;
using HutongGames.PlayMaker;
using MSCLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
using WootingAnalogSDKZ;
using static AnalogKeyboardMSC.Statics;

namespace AnalogKeyboardMSC
{
    public class AnalogKeyboardMSC : Mod
    {
        public override string ID => "AnalogKeyboardMSC"; // Your (unique) mod ID 
        public override string Name => "AnalogKeyboard"; // Your mod name
        public override string Author => "zec"; // Name of the Author (your name)
        public override string Version => "1.0"; // Version
        public override string Description => ""; // Short description of your mod 
        public override Game SupportedGames => Game.MySummerCar_And_MyWinterCar;
        bool inMenuPrev = false;


        public override void ModSetup()
        {
            SetupFunction(Setup.OnLoad, Mod_OnLoad);
            SetupFunction(Setup.PostLoad, Mod_PostLoad);
            SetupFunction(Setup.Update, Mod_Update);
        }

        // Called once, when mod is loading after game is fully loaded
        private void Mod_OnLoad()
        {
            var harmony = HarmonyInstance.Create("AnalogKeyboardMSC");
            harmony.PatchAll();

            //setting a custom path to wooting_analog_wrapper
            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
                static extern IntPtr LoadLibrary(string lpFileName);

#pragma warning disable CS8321 // Local function is declared but never used
            [DllImport("wooting_analog_wrapper", EntryPoint = "wooting_analog_initialise")]
            static extern int initialise();
#pragma warning restore CS8321

            string executingDllPath = Assembly.GetExecutingAssembly().Location;
            string modDirectory = Path.GetDirectoryName(executingDllPath);
            string absoluteDllPath = Path.Combine(modDirectory, @"References\AnalogKeyboard\wooting_analog_wrapper.dll");

            if (File.Exists(absoluteDllPath))
            {
                IntPtr handle = LoadLibrary(absoluteDllPath);
                if (handle == IntPtr.Zero) return;
            }
            else
            {
                ModConsole.Error("Couldn't find wooting_analog_wrapper.dll");
                return;
            }

            //wooting init
            int result1 = WootingAnalogSDK.Initialise(out WootingAnalogResult result2);
            if (result1 == 1)
            {
                IsWootingSDKActive = true;
                ModConsole.Print("wooting loaded");
            }
            else
            {
                ModConsole.Print("wooting error1: " + result1);
                ModConsole.Print("wooting error2: " + result2.ToString());
            }
        }

        private void Mod_PostLoad()
        {
            if (IsWootingSDKActive == false) return;
            ModConsole.Print("wooting postload, active= " + IsWootingSDKActive);
            GetKeybinds();
        }

        // Update is called once per frame
        private void Mod_Update()
        {
            if (IsWootingSDKActive == false) return;

            //update keybinds if player closes a menu
            bool inMenu = FsmVariables.GlobalVariables.FindFsmBool("PlayerInMenu").Value;
            if (inMenu != inMenuPrev && inMenu == false) GetKeybinds();
            inMenuPrev = inMenu;
        }

        public void GetKeybinds()
        {
            ModConsole.Print("getting keybinds");
            Type targetType = typeof(cInput);
            FieldInfo nameField = targetType.GetField("_inputName", BindingFlags.NonPublic | BindingFlags.Static);
            FieldInfo inputsField = targetType.GetField("_inputPrimary", BindingFlags.NonPublic | BindingFlags.Static);

            string[] inputNames = (string[])nameField.GetValue(null);
            KeyCode[] inputsKeys = (KeyCode[])inputsField.GetValue(null);

            List<int> names2 = [];
            if (inputNames != null)
            {
                names2 = [.. inputNames
                    .Select((value, index) => new { value, index })
                    .Where(pair => pair.index < 51)
                    .Select(pair => pair.index)];
            }

            foreach (int num in names2)
            {
                string control = inputNames[num];
                switch (control)
                {
                    case "ThrottleOn":
                        ZForwardKey = Convert.ConvertKeyCode(inputsKeys[num]);
                        break;
                    case "BrakeOn":
                        ZReverseKey = Convert.ConvertKeyCode(inputsKeys[num]);
                        break;
                    case "Left":
                        ZLeftKey = Convert.ConvertKeyCode(inputsKeys[num]);
                        break;
                    case "Right":
                        ZRightKey = Convert.ConvertKeyCode(inputsKeys[num]);
                        break;
                    case "Handbrake":
                        ZHandbrakeKey = Convert.ConvertKeyCode(inputsKeys[num]);
                        break;
                    case "ClutchOn":
                        ZClutchKey = Convert.ConvertKeyCode(inputsKeys[num]);
                        break;
                    case "PlayerLeft":
                        ZPlayerLeftKey = Convert.ConvertKeyCode(inputsKeys[num]);
                        break;
                    case "PlayerRight":
                        ZPlayerRightKey = Convert.ConvertKeyCode(inputsKeys[num]);
                        break;
                    case "PlayerUp":
                        ZPlayerUpKey = Convert.ConvertKeyCode(inputsKeys[num]);
                        break;
                    case "PlayerDown":
                        ZPlayerDownKey = Convert.ConvertKeyCode(inputsKeys[num]);
                        break;
                    default:
                        break;
                }
                //ModConsole.Log(num + " = " + inputNames[num] + " = " + inputsKeys[num].ToString());
            }
        }
    }

    [HarmonyPatch(typeof(cInput), "GetAxisRaw", [typeof(string)])]
    public static class Patch_cInput_GetAxisRaw
    {
        public static bool Prefix(string description, ref float __result)
        {
            if (IsWootingSDKActive == false) return true;

            switch (description)
            {
                case "Throttle":
                    __result = WootingAnalogSDK.ReadAnalog(ZForwardKey, out _);
                    if (__result < 0) return true; //if read fails then fallback to game code
                    return false;
                case "Brake":
                    __result = WootingAnalogSDK.ReadAnalog(ZReverseKey, out _); ;
                    if (__result < 0) return true;
                    return false;
                case "Horizontal": //car steering
                    float left = WootingAnalogSDK.ReadAnalog(ZLeftKey, out _);
                    float right = WootingAnalogSDK.ReadAnalog(ZRightKey, out _);
                    if (left < 0 || right < 0) return true;
                    __result = right - left;
                    return false;
                case "Handbrake":
                    __result = WootingAnalogSDK.ReadAnalog(ZHandbrakeKey, out _); ;
                    if (__result < 0) return true;
                    return false;
                case "Clutch":
                    __result = WootingAnalogSDK.ReadAnalog(ZClutchKey, out _); ;
                    if (__result < 0) return true;
                    return false;
                default:
                    break;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(cInput), "GetAxis", [typeof(string)])]
    public static class Patch_cInput_GetAxis
    {
        public static bool Prefix(string description, ref float __result)
        {
            if (IsWootingSDKActive == false) return true;

            switch (description)
            {
                case "Horizontal": //boat steering
                    float left1 = WootingAnalogSDK.ReadAnalog(ZLeftKey, out _);
                    float right1 = WootingAnalogSDK.ReadAnalog(ZRightKey, out _);
                    if (left1 < 0 || right1 < 0) return true;
                    __result = right1 - left1;
                    return false;
                case "PlayerHorizontal":
                    float left2 = WootingAnalogSDK.ReadAnalog(ZPlayerLeftKey, out _);
                    float right2 = WootingAnalogSDK.ReadAnalog(ZPlayerRightKey, out _);
                    if (left2 < 0 || right2 < 0) return true;
                    __result = right2 - left2;
                    return false;
                case "PlayerVertical":
                    float up = WootingAnalogSDK.ReadAnalog(ZPlayerUpKey, out _);
                    float down = WootingAnalogSDK.ReadAnalog(ZPlayerDownKey, out _);
                    if (up < 0 || down < 0) return true;
                    __result = up - down;
                    return false;
                default:
                    break;
            }
            return true;
        }
    }
}
