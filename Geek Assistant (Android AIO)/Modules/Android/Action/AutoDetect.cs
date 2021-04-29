﻿using System;
using System.Windows.Forms; 
using GeekAssistant.Forms;
using Managed.Adb;

        // (ROOT) 
        // Remove screen lock: "adb shell su -c rm /data/system/*.key" && "adb shell su -c rm /data/system/locksettings*"
        // Hot reboot: "adb shell su -c busybox killall system_server"
        // Clear dulvik cache: "adb shell su -c rm -R /data/dalvik-cache"
        // Factory Reset: "adb shell su -c recovery --wipe_data"

internal static partial class AutoDetect {

    private static Home Home = null;
    public static void Run(bool Silent = false) {
        //Get current Home instance 
        foreach (Form h in Application.OpenForms)
            if (h.GetType() == typeof(Home))
                Home = (Home)h;
         

        inf.currentTitle = "Auto Detect";
        c.Working = true;
        inf.detail.code = "AD-00";  // adb Auto - Begin
        if (!Silent)
            GA_Log.LogEvent("Auto Detect", 2);
        Home.bar.Value = 0;
        try {
            if (!Silent) GA_SetProgressText.Run("Clearing previous device information.", -1);
            inf.detail = ("AD-CD", inf.lvls.FatalError, inf.currentTitle, "We had trouble while clearing previous device information.", null); // Auto Detect - Clear Device
            GA_adb_Functions.ResetDeviceInfo();

            Home.bar.Value = 2;
            if (!Silent) GA_SetProgressText.Run("c.Preparing the environment... Please be patient.", -1);
            inf.detail = ("AD-PE", inf.lvls.FatalError, inf.currentTitle, "Things didn't go as planned while preparing the environment.", null); // Auto Detect - Prepare Environment
            madb.madbBridge();

            Home.bar.Value = 5;
            if (!Silent) GA_SetProgressText.Run("Counting the connected devices...", -1);
            inf.detail = ("AD-Dc", inf.lvls.FatalError, inf.currentTitle, "Math...oh no we couldn't count the devices.", null); // Auto Detect - Device count X
            switch (madb.GetDeviceCount()) {
            case 0: {
                Home.DeviceState_Label.Text = "Disconnected";
                inf.detail = ("AD-D0", inf.lvls.Warn, inf.currentTitle, $"We haven't found any device.\n{prop.strings.TroubleshootConnection}", null); // Auto Detect - Device 0 (0 devices connected) 
                throw new Exception();
            }

            case var @case when @case > 1: {
                Home.DeviceState_Label.Text = "Multiple";
                inf.detail = ("AD-DX", inf.lvls.Warn, inf.currentTitle, $"Oh there are several devices.\nWould you mind keeping 1 and disconnecting the rest please?", null); // Auto Detect - Device X-number (More than 1 connected)
                throw new Exception();
            }
            }

            Home.bar.Value = 7;
            if (!Silent) GA_SetProgressText.Run("Communicating with your device...", -1);
            inf.detail = ("AD-Ds", inf.lvls.Warn, inf.currentTitle, "We have trouble reading your device.", null); // Auto Detect - Device count (failed to count devices)
            string DeviceState_String = "Device is ";
            switch (madb.GetDeviceState()) {
            case 5: // unknown
                {
                Home.DeviceState_Label.Text = "Unknown";
                DeviceState_String += $"in an unknown state...\n{prop.strings.TroubleshootConnection}";
                inf.detail = ("AD-DU", inf.lvls.Information, inf.currentTitle, DeviceState_String, null); // Auto Detect - Device 0 (No devices connected) 
                throw new Exception();
            }

            case 2: // offline
                {
                Home.DeviceState_Label.Text = "Offline";
                DeviceState_String += $"offline. \n{prop.strings.TroubleshootConnection}";
                inf.detail = ("AD-DO", inf.lvls.Warn, inf.currentTitle, DeviceState_String, null); // Auto Detect - Device Offline (PC not allowed to debug device) 
                throw new Exception();
            }

            case 0: // recovery
                {
                Home.DeviceState_Label.Text = "Recovery mode";
                DeviceState_String += $"in recovery mode.\nPlease enter adb mode and try again."; // Please enter adb mode or reboot to system and try again."
                inf.detail = ("AD-DR", inf.lvls.Warn, inf.currentTitle, DeviceState_String, null); // Auto Detect - Device Recovery 
                throw new Exception();
            }

            case 4: // download
                {
                Home.DeviceState_Label.Text = "Download mode";
                DeviceState_String += $"in download mode.\nPlease enter adb mode and try again.";
                inf.detail = ("AD-DD", inf.lvls.Warn, inf.currentTitle, DeviceState_String, null); // Auto Detect - Device Download 
                throw new Exception();
            }

            // ^^   All the above will jump to > Catch e As Exception   ^^
            // vv            All the below will continue code           vv

            case 1: // bootloader 
                {
                DeviceState_String += $"in fastboot mode.";
                Home.DeviceState_Label.Text = "Fastboot mode";
                inf.detail.code = "AD-DF"; // Auto Detect - Device Fastboot
                Home.bar.Value = 10;
                if (!Silent) {
                    var DeviceInFastboot_ContinueAsk = 
                        inf.Run(inf.lvls.Question, $"{DeviceState_String}",
                                  $"We cannot read much in this mode.\nDo you want to continue detection in fastboot mode?",
                                ("Continue", "Close"));
                    if (DeviceInFastboot_ContinueAsk) {
                        inf.detail.lvl = inf.lvls.Error;
                        inf.Run(inf.lvls.Error, inf.currentTitle,
                               $"Oh no this is currently unavailable.\n{prop.strings.FeatureUnavailable}");
                    }
                    // later maybe will be implemented 
                    // ''''''''''''''''''''''''''''''''

                    else {
                        GA_SetProgressText.Run("Detection cancelled by user.", 0);
                    }
                }

                break;
            }

            case 3: // online
                {
                Home.bar.Value = 10;
                inf.detail = ("AD-DlX", inf.lvls.Error, inf.currentTitle, "Sorry we can't see your device anymore...", null);// Auto Detect - Device list X
                Device dev = madb.GetListOfDevice()[0]; // Set dev as first device
                DeviceState_String += $"in adb mode.";
                Home.DeviceState_Label.Text = "Connected (ADB)";
                Home.bar.Value = 11;

                inf.detail = ("AD-D-m", inf.lvls.Error, inf.currentTitle, "Failed to check your device manufacturer information.", null); // Auto Detect - Device - manufacturer 
                if (!Silent) GA_SetProgressText.Run("Fetching device manufacturer, model, and codename...", -1);
                var Manufacturer_CommandResultReceiver = new CommandResultReceiver();
                Home.bar.Value = 13;
                dev.ExecuteShellCommand($"getprop ro.product.manufacturer", Manufacturer_CommandResultReceiver);
                Home.bar.Value = 15;
                c.S.DeviceManufacturer = GA_adb_Functions.FixManufacturerString((string)Manufacturer_CommandResultReceiver.Result);
                c.S.Save();
                Home.Manufacturer_ComboBox.Text = c.S.DeviceManufacturer;

                inf.detail = ("AD-D-mc", inf.lvls.Error, inf.currentTitle, "Failed to check your device model or codename.", null);// Auto Detect - Device - model codename 
                if (!Silent) GA_Log.LogAppendText($" ❱ {c.S.DeviceManufacturer} {dev.Model} ({dev.Product})", 1);
                Home.bar.Value = 17;

                inf.detail = ("AD-D-s", inf.lvls.Error, inf.currentTitle, "Failed to check your device serial number.", null);// Auto Detect - Device - serial 
                if (!Silent) GA_SetProgressText.Run("Fetching device serial#...", -1);
                c.S.DeviceSerial = dev.SerialNumber;
                c.S.Save();
                if (!Silent) GA_Log.LogAppendText($" | Serial: {c.S.DeviceSerial}", 1);
                Home.bar.Value = 20;

                inf.detail = ("AD-D-su", inf.lvls.Error, inf.currentTitle, "Failed to check your device root status.", null);// Auto Detect - Device - su
                if (!Silent) GA_SetProgressText.Run("Fetching root state...", -1);
                c.S.DeviceRooted = dev.CanSU();
                c.S.Save();
                Home.Rooted_Checkbox.Checked = c.S.DeviceRooted;
                if (!Silent) GA_Log.LogAppendText($" | Rooted: {txt.ConvertBoolToYesNo(c.S.DeviceRooted)}", 1);
                Home.bar.Value = 23;

                inf.detail = ("AD-D-bb", inf.lvls.Error, inf.currentTitle, "Failed to check your device busybox availability.", null);// Auto Detect - Device - busybox 
                if (!Silent) GA_SetProgressText.Run("Fetching busybox availability...", -1);
                c.S.DeviceBusyBoxReady = dev.BusyBox.Available;
                c.S.Save();
                if (!Silent) GA_Log.LogAppendText($" | Busybox available: {txt.ConvertBoolToYesNo(c.S.DeviceBusyBoxReady)}", 1);
                Home.bar.Value = 25;

                inf.detail = ("AD-D-blu", inf.lvls.Error, inf.currentTitle, "Failed to check your device bootloader unlock support.", null); // Auto Detect - Device - bootloader unlock 
                if (!Silent) GA_SetProgressText.Run("Fetching bootloader unlock support state...", -1);
                var BLunlockable_CommandResultReceiver = new CommandResultReceiver();
                Home.bar.Value = 26;
                dev.ExecuteShellCommand($"getprop ro.oem_unlock_supported", BLunlockable_CommandResultReceiver); 
                c.S.DeviceBootloaderUnlockSupported = txt.ConvertIntToBool(BLunlockable_CommandResultReceiver.Result);
                c.S.Save();
                Home.BootloaderUnlockable_CheckBox.Checked = c.S.DeviceBootloaderUnlockSupported;
                Home.bar.Value = 27;
                if (!Silent) GA_Log.LogAppendText($" | Bootloader unlock allowed: {txt.ConvertBoolToYesNo(c.S.DeviceBootloaderUnlockSupported)}", 1);
                Home.bar.Value = 30;

                inf.detail = ("AD-D-al", inf.lvls.Error, inf.currentTitle, "Failed to check your device API level.", null);// Auto Detect - Device - API level  
                if (!Silent) GA_SetProgressText.Run("Fetching Android API level...", -1);
                var API_CommandResultReceiver = new CommandResultReceiver();
                Home.bar.Value = 31;
                dev.ExecuteShellCommand($"getprop {Device.PROP_BUILD_API_LEVEL}", API_CommandResultReceiver);
                Home.bar.Value = 32;
                c.S.DeviceAPILevel = Convert.ToInt32(API_CommandResultReceiver.Result);
                c.S.Save();
                inf.detail = ("AD-D-atv", inf.lvls.Error, inf.currentTitle, "Failed to convert the API level to Android version.", null);// Auto Detect - Device - API level version
                Home.AndroidVersion_ComboBox.Text = GA_adb_Functions.ConvertAPILevelToAVer(c.S.DeviceAPILevel)[1];
                if (!Silent) GA_SetProgressText.Run("Converting API level to Android name...", -1);
                Home.bar.Value = 33;
                if (!Silent) GA_Log.LogAppendText($" | Android version: {GA_adb_Functions.ConvertAPILevelToAVer(c.S.DeviceAPILevel)[0]} (API: {c.S.DeviceAPILevel})", 1);
                Home.bar.Value = 35;

                inf.detail = ("AD-D-b", inf.lvls.Error, inf.currentTitle, "Failed to check your device battery level.", null); // Auto Detect - Device - battery 
                if (!Silent) GA_SetProgressText.Run("Fetching battery level...", -1);
                Home.bar.Value = 36;
                string batteryString;
                if (dev.GetBatteryInfo().Present) {
                    c.S.DeviceBatteryLevel = dev.GetBatteryInfo().Level;
                    batteryString = $"{c.S.DeviceBatteryLevel}%";
                } else {
                    c.S.DeviceBatteryLevel = -1; // no level. Not present
                    if (!Silent)
                        GA_SetProgressText.Run("Battery not present!", -1);
                    batteryString = "❌";
                }
                c.S.Save();
                Home.bar.Value = 38;
                if (!Silent)
                    GA_Log.LogAppendText($" | Battery: {batteryString}", 1);
                Home.bar.Value = 40;
                if (!Silent)
                    GA_SetProgressText.Run(DeviceState_String, -1); // This is after retrieving info to stay written in Progress Text
                break;
            }
            }

            Home.bar.Value = 100;
        } catch (Exception ex) {
            GA_PleaseWait.Run(false); // Close before error dialog
            if (!Silent) {
                inf.Run(inf.detail.lvl, inf.detail.title, inf.detail.msg, ex.ToString());
            } else {
                Home.DoNeutral();
            }
        }

        c.S.DeviceState = Home.DeviceState_Label.Text;
        GA_PleaseWait.Run(false); // Close if Try was successful
        c.Working = false;
    }
}