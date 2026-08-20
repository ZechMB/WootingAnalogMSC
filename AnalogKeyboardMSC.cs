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
        

        public override void ModSetup()
        {
            SetupFunction(Setup.OnLoad, Mod_OnLoad);
            SetupFunction(Setup.PostLoad, Mod_PostLoad);
            SetupFunction(Setup.OnGUI, Mod_OnGUI);
            SetupFunction(Setup.Update, Mod_Update);
            SetupFunction(Setup.FixedUpdate, Mod_FixedUpdate);
            SetupFunction(Setup.ModSettings, Mod_Settings);
        }

        private void Mod_Settings()
        {
            // All settings should be created here. 
            // DO NOT put anything that isn't settings or keybinds in here!
        }

        // Called once, when mod is loading after game is fully loaded
        private void Mod_OnLoad()
        {
            var harmony = HarmonyInstance.Create("AnalogKeyboardMSC");
            harmony.PatchAll();

            //getting and setting the path for all calls to wooting_analog_wrapper
            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            static extern IntPtr LoadLibrary(string lpFileName);

            const string SdkLib = "wooting_analog_wrapper";

            [DllImport(SdkLib, EntryPoint = "wooting_analog_initialise")]
            static extern int initialise();

            string executingDllPath = Assembly.GetExecutingAssembly().Location;
            string modDirectory = Path.GetDirectoryName(executingDllPath);
            string absoluteDllPath = Path.Combine(modDirectory, @"analog\wooting_analog_wrapper.dll");

            if (File.Exists(absoluteDllPath))
            {
                IntPtr handle = LoadLibrary(absoluteDllPath);
                if (handle == IntPtr.Zero) return;
            }


            int result1 = WootingAnalogSDK.Initialise(out WootingAnalogResult result2);
            if (result1 == 1)
            {
                IsWootingSDKActive = true;
                ModConsole.Print("wooting loaded");
            }
            else
            {
                ModConsole.Print("wooting failed");
            }
        }

        private void Mod_PostLoad()
        {
            if (IsWootingSDKActive != true) return;
            ModConsole.Print("wooting postload");
            List<string> names = [];
            List<int> names2 = [];

            Type targetType = typeof(cInput);

            
            FieldInfo nameField = targetType.GetField("_inputName", BindingFlags.NonPublic | BindingFlags.Static);
            FieldInfo inputsField = targetType.GetField("_inputPrimary", BindingFlags.NonPublic | BindingFlags.Static);

            // 3. Read the values (pass 'null' as the argument because the fields are static)
            string[] inputName = (string[])nameField.GetValue(null);
            KeyCode[] inputsKeys = (KeyCode[])inputsField.GetValue(null);


            if (inputName != null)
            {
                ModConsole.Print("postload1");
                int[] leftPositions = inputName
                    .Select((value, index) => new { value, index })
                    .Where(pair => pair.value == "ThrottleOn" || pair.value == "BrakeOn" || pair.value == "Left" || pair.value == "Right" || pair.value == "Handbrake" || pair.value == "ClutchOn")
                    .Select(pair => pair.index)
                    .ToArray();
                names2 = [.. leftPositions];
            }

            foreach (int num in names2)
            {
                string control = inputName[num];
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
                ModConsole.Log("2= " + num);
                ModConsole.Log("3= " + inputName[num]);
                ModConsole.Log("4= " + inputsKeys[num].ToString());
            }
            ModConsole.Print("found4 " + names2.Count);
        }

        // Draw unity OnGUI() here
        private void Mod_OnGUI()
        {
            // 1. Draw a simple background box
            //GUI.Box(new Rect(10, 10, 160, 120), "Debug Menu");

            // 2. Draw static text
            //GUI.Label(new Rect(20, 40, 140, 20), $"1= {name}");
            //GUI.Label(new Rect(20, 60, 140, 20), $"2 = {name2}");
            //GUI.Label(new Rect(20, 40, 140, 20), $"a = {Left}");
            //GUI.Label(new Rect(20, 60, 140, 20), $"d = {Right}");
        }

        // Update is called once per frame
        private void Mod_Update()
        {
            if (IsWootingSDKActive != true) return;

            //name = cInput.GetAxisRaw("Throttle").ToString();

            string currentCar = FsmVariables.GlobalVariables.FindFsmString("PlayerCurrentVehicle").Value;
            name2 = currentCar;

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

        //called every physics tick
        private void Mod_FixedUpdate()
        {

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
