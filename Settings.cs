using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

#pragma warning disable CS8600, CS8602, CS8603

namespace Proxy3
{
    public class SettingsManager
    {
        public string vSource = "";
        public string AblyAPI = "";
        public string STUN = "Google";
        public string TwilioSID = "";
        public string TwilioAuth = "";
        public void LoadSettings()
        {
            string getvSource = SettingsManager.GetSetting("vSource") as string;
            if (getvSource == null) { SettingsManager.SaveSetting("vSource", vSource); } else { vSource = getvSource; }

            string getAblyAPI = SettingsManager.GetSetting("AblyAPI") as string;
            if (getAblyAPI == null) { SettingsManager.SaveSetting("AblyAPI", AblyAPI); } else { AblyAPI = getAblyAPI; }

            string stunSetting = SettingsManager.GetSetting("STUN") as string;
            if (stunSetting == null) { SettingsManager.SaveSetting("STUN", STUN); } else { STUN = stunSetting; }

            string getTwilioSID = SettingsManager.GetSetting("TwilioSID") as string;
            if (getTwilioSID == null) { SettingsManager.SaveSetting("TwilioSID", TwilioSID); } else { TwilioSID = getTwilioSID; }

            string getTwilioAuth = SettingsManager.GetSetting("TwilioAuth") as string;
            if (getTwilioAuth == null) { SettingsManager.SaveSetting("TwilioAuth", TwilioAuth); } else { TwilioAuth = getTwilioAuth; }
        }

        public static void SaveSetting(string settingName, string value)
        {
            string companyName = "Result-3";
            string applicationName = "Proxy3";
            RegistryKey key = Registry.CurrentUser.CreateSubKey($"Software\\{companyName}\\{applicationName}");
            key.SetValue(settingName, value);
            key.Close();
        }
        public static void SaveSetting(string settingName, int value)
        {
            string companyName = "Result-3";
            string applicationName = "Proxy3";
            RegistryKey key = Registry.CurrentUser.CreateSubKey($"Software\\{companyName}\\{applicationName}");
            key.SetValue(settingName, value, RegistryValueKind.DWord);
            key.Close();
        }
        public static object GetSetting(string settingName)
        {
            string companyName = "Result-3";
            string applicationName = "Proxy3";
            using (RegistryKey readKey = Registry.CurrentUser.OpenSubKey($"Software\\{companyName}\\{applicationName}"))
            {
                object readValue = readKey?.GetValue(settingName);
                if (readValue != null)
                {
                    RegistryValueKind valueKind = readKey.GetValueKind(settingName);
                    switch (valueKind)
                    {
                        case RegistryValueKind.String:
                            return readValue.ToString();
                        case RegistryValueKind.DWord:
                            return Convert.ToInt32(readValue);
                        default:
                            throw new NotSupportedException($"Unsupported registry value kind: {valueKind}");
                    }
                }
                return null;
            }
        }
        public async Task GetVideoDevices(ComboBox Source)
        {
            Source.Items.Add(new ComboBoxItem { Content = vSource, Tag = vSource });
            var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            if (devices.Any())
            {
                foreach (var device in devices) 
                {
                    Source.Items.Add(new ComboBoxItem { Content = device.Name, Tag = device.Name });
                }
            }
        }
        public Dictionary<string, string> GetSerialPorts()
        {
            Dictionary<string, string> serialPorts = new Dictionary<string, string>();
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption LIKE '%(COM%' AND Caption LIKE '%USB%'");
                foreach (ManagementObject queryObj in searcher.Get())
                {
                    string caption = queryObj["Caption"]?.ToString() ?? string.Empty;
                    string device = queryObj["DeviceID"]?.ToString() ?? string.Empty;

                    int comIndex = caption.IndexOf("(COM");
                    if (comIndex >= 0)
                    {
                        string portName = caption.Substring(comIndex + 1, caption.IndexOf(")", comIndex) - comIndex - 1);
                        serialPorts.Add(portName, device);
                    }
                }
            }
            catch { }
            return serialPorts;
        }
    }
}
