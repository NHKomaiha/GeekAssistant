﻿using GeekAssistant.Forms;
using Managed.Adb;
using System;
using System.Windows.Forms;

// (ROOT)
// Remove screen lock: $"{workCode_init}b shell su -c rm /data/system/*.key" && $"{workCode_init}b shell su -c rm /data/system/locksettings*"
// Hot reboot: $"{workCode_init}b shell su -c busybox killall system_server"
// Clear dulvik cache: $"{workCode_init}b shell su -c rm -R /data/dalvik-cache"
// Factory Reset: $"{workCode_init}b shell su -c recovery --wipe_data"
// Root check: << "adb shell su" example >> "su: not found" 
// Root check: << set uid= for /f "delims=" %%a in ('adb -s devicename shell "su 0 id -u 2>/dev/null"') do set uid=%%a && echo %uid%
//             >> 0 or 1

internal class AutoDetect {
    private static string workCode_init => "AD";
    private static string workTitle => "Auto Detect";

    private static bool RunningInBetween = false;
    public static async void Run(bool Silent = false) {
        //Refresh current Home instance
        Home Home = null;
        foreach (Form home in Application.OpenForms)
            if (home.GetType() == typeof(Home))
                Home = (Home)home;
        RunningInBetween = c.Working; //true if auto detecting while running another process
        c.Working = true;

        if (!RunningInBetween) inf.workTitle = workTitle;
        inf.detail.workCode =
            $"{(RunningInBetween ? $"{inf.detail.workCode}-" : "")}" + $"{workCode_init}-00"; // (RunningInBetween? {workCode_init} -) Auto Detect - Begin
        if (!Silent) Log.LogEvent(workTitle, 2);

        try {
            Home.bar.Value = 0;
            if (!Silent) SetProgressText.Run("Clearing previous device information.", -1);
            inf.detail = ($"{workCode_init}-CD", inf.lvls.FatalError, inf.workTitle, "We had trouble while clearing previous device information.", null); // Auto Detect - Clear Device
            GA_adb.ResetDeviceInfo();

            Home.bar.Value = 2;
            if (!Silent) SetProgressText.Run("c.Preparing the environment... Please be patient.", -1);

            inf.detail = ($"{workCode_init}-PE", inf.lvls.FatalError, inf.workTitle, "Things didn't go as planned while preparing the environment.", null); // Auto Detect - Prepare Environment
            await madb.madbBridge();

            Home.bar.Value = 5;
            if (!Silent) SetProgressText.Run("Counting the connected devices...", -1);

            inf.detail = ($"{workCode_init}-Dc", inf.lvls.FatalError, inf.workTitle, "Math...oh no we couldn't count the devices.", null); // Auto Detect - Device count X
            switch (madb.GetDeviceCount().Result) {
                case 0:
                    Home.DeviceState_Label.Text = "Disconnected";
                    inf.detail = ($"{workCode_init}-D0", inf.lvls.Warn, inf.workTitle, $"We haven't found any device.\n{prop.strings.TroubleshootConnection}", null); // Auto Detect - Device 0 (0 devices connected)
                    throw new Exception();
                case > 1:
                    Home.DeviceState_Label.Text = "Multiple";
                    inf.detail = ($"{workCode_init}-DX", inf.lvls.Warn, inf.workTitle, $"Oh there are several devices.\nWould you mind keeping 1 and disconnecting the rest please?", null); // Auto Detect - Device X-number (More than 1 connected)
                    throw new Exception();
            }

            Home.bar.Value = 7;
            if (!Silent) SetProgressText.Run("Communicating with your device...", -1);

            inf.detail = ($"{workCode_init}-Ds", inf.lvls.Warn, inf.workTitle, "We have trouble reading your device.", null); // Auto Detect - Device count (failed to count devices)
            string DeviceState_String = "Device is ";
            switch (madb.GetDeviceState().Result) {
                case 5: // unknown 
                    Home.DeviceState_Label.Text = "Unknown";
                    DeviceState_String += $"in an unknown state...\n{prop.strings.TroubleshootConnection}";
                    inf.detail = ($"{workCode_init}-DU", inf.lvls.Information, inf.workTitle, DeviceState_String, null); // Auto Detect - Device 0 (No devices connected)
                    throw new Exception();
                case 2: // offline 
                    Home.DeviceState_Label.Text = "Offline";
                    DeviceState_String += $"offline. \n{prop.strings.TroubleshootConnection}";
                    inf.detail = ($"{workCode_init}-DO", inf.lvls.Warn, inf.workTitle, DeviceState_String, null); // Auto Detect - Device Offline (PC not allowed to debug device)
                    throw new Exception();
                case 0: // recovery 
                    Home.DeviceState_Label.Text = "Recovery mode";
                    DeviceState_String += $"in recovery mode.\nPlease enter adb mode and try again."; // Please enter adb mode or reboot to system and try again."
                    inf.detail = ($"{workCode_init}-DR", inf.lvls.Warn, inf.workTitle, DeviceState_String, null); // Auto Detect - Device Recovery
                    throw new Exception();
                case 4: // download 
                    Home.DeviceState_Label.Text = "Download mode";
                    DeviceState_String += $"in download mode.\nPlease enter adb mode and try again.";
                    inf.detail = ($"{workCode_init}-DD", inf.lvls.Warn, inf.workTitle, DeviceState_String, null); // Auto Detect - Device Download
                    throw new Exception();

                // ^^   All the above will jump to >> catch (Exception ex) { >>  ^^
                // vv              All the below will continue code              vv

                case 1: // bootloader 
                    DeviceState_String += $"in fastboot mode.";
                    Home.DeviceState_Label.Text = "Fastboot mode";
                    inf.detail.workCode = $"{workCode_init}-DF"; // Auto Detect - Device Fastboot
                    Home.bar.Value = 10;
                    if (!Silent) {
                        var DeviceInFastboot_ContinueAsk =
                            inf.Run(inf.lvls.Question, $"{DeviceState_String}",
                                      $"We cannot read much in this mode.\nDo you want to continue detection in fastboot mode?",
                                    ("Continue", "Close"));
                        if (DeviceInFastboot_ContinueAsk) {
                            inf.detail.lvl = inf.lvls.Error;
                            inf.Run(inf.lvls.Error, inf.workTitle,
                                   $"Oh no this is currently unavailable.\n{prop.strings.FeatureUnavailable}");
                        }
                        // later maybe will be implemented
                        // ''''''''''''''''''''''''''''''''
                        else {
                            SetProgressText.Run("Detection cancelled by user.", 0);
                        }
                    }
                    break;

                case 3: // online 
                    Home.bar.Value = 10;
                    inf.detail = ($"{workCode_init}-DlX", inf.lvls.Error, inf.workTitle, "Sorry we can't see your device anymore...", null);// Auto Detect - Device list X
                    Device dev = madb.GetListOfDevice().Result[0]; // Set dev as first device
                    DeviceState_String += $"in adb mode.";
                    Home.DeviceState_Label.Text = "Connected (ADB)";
                    Home.bar.Value = 11;

                    inf.detail = ($"{workCode_init}-D-m", inf.lvls.Error, inf.workTitle, "Failed to check your device manufacturer information.", null); // Auto Detect - Device - manufacturer
                    if (!Silent)
                        SetProgressText.Run("Fetching device manufacturer, model, and codename...", -1);

                    Home.bar.Value = 13;

                    Home.bar.Value = 15;
                    c.S.DeviceManufacturer =
                        GA_adb.FixManufacturerString(
                            cmd.madbShell(dev, "getprop ro.product.manufacturer"));
                    c.S.Save();
                    Home.Manufacturer_ComboBox.Text = c.S.DeviceManufacturer;

                    inf.detail = ($"{workCode_init}-D-mc", inf.lvls.Error, inf.workTitle, "Failed to check your device model or codename.", null);// Auto Detect - Device - model codename
                    if (!Silent)
                        Log.LogAppendText($" ❱ {c.S.DeviceManufacturer} {dev.Model} ({dev.Product})", 1);

                    Home.bar.Value = 17;

                    inf.detail = ($"{workCode_init}-D-s", inf.lvls.Error, inf.workTitle, "Failed to check your device serial number.", null);// Auto Detect - Device - serial
                    if (!Silent)
                        SetProgressText.Run("Fetching device serial#...", -1);

                    c.S.DeviceSerial = dev.SerialNumber;
                    c.S.Save();
                    if (!Silent)
                        Log.LogAppendText($" | Serial: {c.S.DeviceSerial}", 1);

                    Home.bar.Value = 20;

                    inf.detail = ($"{workCode_init}-D-su", inf.lvls.Error, inf.workTitle, "Failed to check your device root status.", null);// Auto Detect - Device - su
                    if (!Silent)
                        SetProgressText.Run("Fetching root state...", -1);

                    c.S.DeviceRooted = dev.CanSU();
                    c.S.Save();
                    Home.Rooted_Checkbox.Checked = c.S.DeviceRooted;
                    if (!Silent) Log.LogAppendText($" | Rooted: {convert.Bool.ToYesNo(c.S.DeviceRooted)}", 1);

                    Home.bar.Value = 23;

                    inf.detail = ($"{workCode_init}-D-bb", inf.lvls.Error, inf.workTitle, "Failed to check your device busybox availability.", null);// Auto Detect - Device - busybox
                    if (!Silent) SetProgressText.Run("Fetching busybox availability...", -1);

                    c.S.DeviceBusyBoxReady = dev.BusyBox.Available;
                    c.S.Save();
                    if (!Silent) Log.LogAppendText($" | Busybox available: {convert.Bool.ToYesNo(c.S.DeviceBusyBoxReady)}", 1);

                    Home.bar.Value = 25;

                    inf.detail = ($"{workCode_init}-D-blu", inf.lvls.Error, inf.workTitle, "Failed to check your device bootloader unlock support.", null); // Auto Detect - Device - bootloader unlock
                    if (!Silent) SetProgressText.Run("Fetching bootloader unlock support state...", -1);

                    Home.bar.Value = 26;
                    c.S.DeviceBootloaderUnlockSupported =
                         convert.String.ToBool(cmd.madbShell(dev, "getprop ro.oem_unlock_supported"));
                    c.S.Save();
                    Home.BootloaderUnlockable_CheckBox.Checked = c.S.DeviceBootloaderUnlockSupported;
                    Home.bar.Value = 27;
                    if (!Silent)
                        Log.LogAppendText($" | Bootloader unlock allowed: {convert.Bool.ToYesNo(c.S.DeviceBootloaderUnlockSupported)}", 1);

                    Home.bar.Value = 30;

                    inf.detail = ($"{workCode_init}-D-al", inf.lvls.Error, inf.workTitle, "Failed to check your device API level.", null);// Auto Detect - Device - API level
                    if (!Silent) SetProgressText.Run("Fetching Android API level...", -1);

                    Home.bar.Value = 32;
                    c.S.DeviceAPILevel = Convert.ToInt32(cmd.madbShell(dev, $"getprop {Device.PROP_BUILD_API_LEVEL}"));
                    c.S.Save();
                    inf.detail = ($"{workCode_init}-D-atv", inf.lvls.Error, inf.workTitle, "Failed to convert the API level to Android version.", null);// Auto Detect - Device - API level version
                    Home.AndroidVersion_ComboBox.Text = GA_adb.ConvertAPILevelToAVer(c.S.DeviceAPILevel)[1];
                    if (!Silent) SetProgressText.Run("Converting API level to Android name...", -1);

                    Home.bar.Value = 33;
                    if (!Silent) Log.LogAppendText($" | Android version: {GA_adb.ConvertAPILevelToAVer(c.S.DeviceAPILevel)[0]} (API: {c.S.DeviceAPILevel})", 1);

                    Home.bar.Value = 35;

                    inf.detail = ($"{workCode_init}-D-b", inf.lvls.Error, inf.workTitle, "Failed to check your device battery level.", null); // Auto Detect - Device - battery
                    if (!Silent) SetProgressText.Run("Fetching battery level...", -1);

                    Home.bar.Value = 36;
                    string batteryString;
                    if (dev.GetBatteryInfo().Present) {
                        c.S.DeviceBatteryLevel = dev.GetBatteryInfo().Level;
                        batteryString = $"{c.S.DeviceBatteryLevel}%";
                    } else {
                        c.S.DeviceBatteryLevel = -1; // no level. Not present
                        if (!Silent) SetProgressText.Run("Battery not present!", -1);

                        batteryString = "❌";
                    }
                    c.S.Save();
                    Home.bar.Value = 38;
                    if (!Silent) Log.LogAppendText($" | Battery: {batteryString}", 1);

                    Home.bar.Value = 40;
                    if (!Silent) SetProgressText.Run(DeviceState_String, -1); // This is after retrieving info to stay written in Progress Text

                    break;
            }

            Home.bar.Value = 100;
        } catch (Exception ex) {
            if (!RunningInBetween) GAwait.Run(false); // Close before error dialog
            if (!Silent) {
                inf.detail.fullFatalError = ex.ToString();
                inf.Run();
            } else
                Home.DoNeutral();
        }

        c.S.DeviceState = Home.DeviceState_Label.Text;
        if (!RunningInBetween) GAwait.Run(false); // Close if Try was successful
        c.Working = RunningInBetween;
    }
}