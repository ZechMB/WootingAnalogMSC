using Harmony;
using HutongGames.PlayMaker;
using MSCLoader;
using Steamworks;
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
#pragma warning restore CS8321 // store the dll in memory

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

            float result1 = 0;
            WootingAnalogResult result2 = WootingAnalogResult.Failure;
            result1 = WootingAnalogSDK.ReadAnalog(ZForwardKey, out result2);
            if (result1 > -1) ZForward = result1;
            result1 = WootingAnalogSDK.ReadAnalog(ZReverseKey, out result2);
            if (result1 > -1) ZReverse = result1;
            result1 = WootingAnalogSDK.ReadAnalog(ZLeftKey, out result2);
            if (result1 > -1) ZLeft = -result1;
            result1 = WootingAnalogSDK.ReadAnalog(ZRightKey, out result2);
            if (result1 > -1) ZRight = result1;
            result1 = WootingAnalogSDK.ReadAnalog(ZClutchKey, out result2);
            if (result1 > -1) ZClutch = result1;
            result1 = WootingAnalogSDK.ReadAnalog(ZHandbrakeKey, out result2);
            if (result1 > -1) ZHandbrake = result1;
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
                ModConsole.Print("postload1");
                int[] leftPositions = inputNames
                    .Select((value, index) => new { value, index })
                    .Where(pair => pair.value == "ThrottleOn" || pair.value == "BrakeOn" || pair.value == "Left" || pair.value == "Right" || pair.value == "Handbrake" || pair.value == "ClutchOn")
                    .Select(pair => pair.index)
                    .ToArray();
                names2 = [.. leftPositions];
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
                    default:
                        break;
                }
                ModConsole.Log("1= " + num);
                ModConsole.Log("2= " + inputNames[num]);
                ModConsole.Log("3= " + inputsKeys[num].ToString());
            }
        }
    }

    [HarmonyPatch(typeof(cInput), "GetAxisRaw", new[] { typeof(string) })]
    public static class Patch_cInput_GetAxisRaw
    {
        [HarmonyPrefix]
        public static bool Prefix(string description, ref float __result)
        {
            if (IsWootingSDKActive == false) return true;

            switch (description)
            {
                case "Throttle":
                    __result = ZForward;
                    return false;
                case "Brake":
                    __result = ZReverse;
                    return false;
                case "Horizontal":
                    __result = ZLeft + ZRight;
                    return false;
                case "Handbrake":
                    __result = ZHandbrake;
                    return false;
                case "Clutch":
                    __result = ZClutch;
                    return false;
                case "PlayerHorizontal": //need to modify charactermotor in unityscriptfirstpass
                    __result = ZLeft + ZRight;
                    return false;
                case "PlayerVertical":
                    __result = ZForward + ZReverse;
                    return false;
                default:
                    break;
            }

            return true;
        }
    }

    //boat uses this instead of raw
    [HarmonyPatch(typeof(cInput), "GetAxis", new[] { typeof(string) })]
    public static class Patch_cInput_GetAxis
    {
        [HarmonyPrefix]
        public static bool Prefix(string description, ref float __result)
        {
            if (IsWootingSDKActive == false) return true;

            switch (description)
            {
                case "Horizontal":
                    __result = ZLeft + ZRight;
                    return false;
                default:
                    break;
            }

            return true;
        }
    }
}
