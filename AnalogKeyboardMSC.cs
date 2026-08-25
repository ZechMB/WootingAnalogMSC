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
        public override string Description => "Control game with analog input"; // Short description of your mod 
        public override Game SupportedGames => Game.MySummerCar_And_MyWinterCar;
        bool inMenuPrev = false;
        SettingsKeybind lockInputBind;
        SettingsCheckBox tabout;


        public override void ModSetup()
        {
            SetupFunction(Setup.OnLoad, Mod_OnLoad);
            SetupFunction(Setup.PostLoad, Mod_PostLoad);
            SetupFunction(Setup.Update, Mod_Update);
            SetupFunction(Setup.ModSettings, Mod_Settings);
            SetupFunction(Setup.OnGUI, Mod_OnGui);
        }

        private void Mod_OnGui()
        {
            if (AreKeysLocked)
            {
                float labelWidth = 300f;
                float labelHeight = 50f;

                float xPosition = (Screen.width / 2f) - (labelWidth / 2f);
                float yPosition = (Screen.height / 2f) - (labelHeight / 2f) - 100f;

                Rect labelRect = new(xPosition, yPosition, labelWidth, labelHeight);

                GUIStyle textStyle = new(GUI.skin.label)
                {
                    fontSize = 22,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };

                string message = "Inputs locked, Unlock with [" + lockInputBind.GetKeybindValue + "]";

                // Draw a shadow first for maximum readability
                textStyle.normal.textColor = Color.black;
                GUI.Label(new Rect(labelRect.x + 2, labelRect.y + 2, labelRect.width, labelRect.height), message, textStyle);

                // Draw the main text over the shadow
                textStyle.normal.textColor = Color.white;
                GUI.Label(new Rect(xPosition, yPosition, labelWidth, labelHeight), message, textStyle);
            }
        }

        private void Mod_Settings()
        {
            lockInputBind = Keybind.Add("ZLockInput", "Lock Input", KeyCode.Y);
            tabout = Settings.AddCheckBox("DisableOnTabOut", "Disable input when tabbed out", true);
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
                ModConsole.Error("wooting error1: " + result1);
                ModConsole.Error("wooting error2: " + result2.ToString());
            }
        }

        private void Mod_PostLoad()
        {
            if (IsWootingSDKActive == false) return;
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

            bool keyDown = lockInputBind.GetKeybindDown();
            if (keyDown)
            {
                AreKeysLocked = !AreKeysLocked;
            }

            if (!AreKeysLocked)
            {
                if (tabout.GetValue() == true)
                {
                    if (WindowCheck.IsGameFocused()) GetKeyValues();
                }
                else GetKeyValues();
            }
        }

        public void GetKeyValues()
        {
            float result1 = 0;
            result1 = WootingAnalogSDK.ReadAnalog(ZForwardKey, out _);
            if (result1 > -1) ZForward = result1;
            result1 = WootingAnalogSDK.ReadAnalog(ZReverseKey, out _);
            if (result1 > -1) ZReverse = result1;
            result1 = WootingAnalogSDK.ReadAnalog(ZLeftKey, out _);
            if (result1 > -1) ZLeft = result1;
            result1 = WootingAnalogSDK.ReadAnalog(ZRightKey, out _);
            if (result1 > -1) ZRight = result1;
            result1 = WootingAnalogSDK.ReadAnalog(ZClutchKey, out _);
            if (result1 > -1) ZClutch = result1;
            result1 = WootingAnalogSDK.ReadAnalog(ZHandbrakeKey, out _);
            if (result1 > -1) ZHandbrake = result1;
            result1 = WootingAnalogSDK.ReadAnalog(ZPlayerLeftKey, out _);
            if (result1 > -1) ZPlayerLeft = result1;
            result1 = WootingAnalogSDK.ReadAnalog(ZPlayerRightKey, out _);
            if (result1 > -1) ZPlayerRight = result1;
            result1 = WootingAnalogSDK.ReadAnalog(ZPlayerUpKey, out _);
            if (result1 > -1) ZPlayerUp = result1;
            result1 = WootingAnalogSDK.ReadAnalog(ZPlayerDownKey, out _);
            if (result1 > -1) ZPlayerDown = result1;
        }

        public void GetKeybinds()
        {
            //ModConsole.Print("getting keybinds");
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
                    __result = ZForward;
                    return false;
                case "Brake":
                    __result = ZReverse;
                    return false;
                case "Horizontal": //car steering
                    __result = ZRight - ZLeft;
                    return false;
                case "Handbrake":
                    __result = ZHandbrake;
                    return false;
                case "Clutch":
                    __result = ZClutch;
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
                    __result = ZRight - ZLeft;
                    return false;
                case "PlayerHorizontal":
                    __result = ZPlayerRight - ZPlayerLeft;
                    return false;
                case "PlayerVertical":
                    __result = ZPlayerUp - ZPlayerDown;
                    return false;
                default:
                    break;
            }
            return true;
        }
    }
}
