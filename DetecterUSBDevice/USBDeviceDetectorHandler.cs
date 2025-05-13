using System;
using System.Diagnostics;
using System.Management;
using System.Text;

namespace DetecterUSBDevice
{
    public class USBDeviceDetectorHandler
    {
        private readonly string _blockVID;
        private readonly string _blockPID;
        private string _blockSN;

        public string _USBDescriptionData;

        public USBDeviceDetectorHandler(string blockVID, string blockPID)
        {
            _blockVID = blockVID;
            _blockPID = blockPID;
        }

        public USBDeviceDetectorHandler(){}

        public void HandleUSBEvent(EventArrivedEventArgs e, string eventType)
        {
            try
            {
                var device = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                string deviceId = device["DeviceID"]?.ToString();
                parseUSBDeviceId(deviceId, out string vid, out string pid, out string serialNumber);

                var USBDescriptionData = new StringBuilder();

                USBDescriptionData.Append($"[{eventType}] Устройство:\n");
                USBDescriptionData.Append($"  - VID: {vid ?? "N/A"}\n");
                USBDescriptionData.Append($"  - PID: {pid ?? "N/A"}\n");
                USBDescriptionData.Append($"  - S/N: {serialNumber ?? "N/A"}\n");
                USBDescriptionData.Append($"  - Description: {device.GetPropertyValue("Description")}\n");
                USBDescriptionData.Append($"  - Manufacturer: {device.GetPropertyValue("Manufacturer")}\n");
                USBDescriptionData.Append($"  - Connection time: {DateTime.Now}\n");


                if (pid == _blockPID && vid == _blockVID)
                {
                    USBDescriptionData.Append("-------Block this USB device-------\n");
                    _blockSN = serialNumber;
                    disableUSBDeviceWithPowerShell(vid, pid);
                }

                USBDescriptionData.Append("----------------------------------\n");
                _USBDescriptionData = USBDescriptionData.ToString();

                USBWatcher.USBFind();
            }
            catch (Exception ex)
            {
                DUSBDLogger.WriteLog(ex.Message);
            }
        }

        public void HandleUSBRestoreEvent(EventArrivedEventArgs e, string eventType)
        {
            try
            {
                var device = (ManagementBaseObject)e.NewEvent["TargetInstance"];
                string deviceId = device["DeviceID"]?.ToString();
                parseRestoreUSBDeviceId(deviceId, out string serialNumber);

                if (serialNumber == _blockSN)
                {
                    DUSBDLogger.WriteLog("-------Block turn on USB device-------\n");
                    disableUSBDeviceWithPowerShell(_blockVID, _blockPID);
                }
            }
            catch (Exception ex)
            {
                DUSBDLogger.WriteLog(ex.Message);
            }
        }

        private static void parseRestoreUSBDeviceId(string deviceId, out string serialNumber)
        {
            serialNumber = null;
            parseUSBDeviceIdCheck(deviceId);
            var parts = deviceId.Split('\\');
            if (parts.Length >= 3)
                serialNumber = parts[2].Substring(0, parts[2].Length - 2);
        }

        private static void parseUSBDeviceId(string deviceId, out string vid, out string pid, out string serialNumber)
        {
            vid = null;
            pid = null;
            serialNumber = null;
            parseUSBDeviceIdCheck(deviceId);
            var parts = deviceId.Split('\\');
            if (parts[1].Contains("VID_") && parts[1].Contains("PID_"))
            {
                var vidPid = parts[1].Split('&');
                vid = vidPid[0].Replace("VID_", "").Trim();
                pid = vidPid[1].Replace("PID_", "").Trim();
            }
            if (parts.Length >= 3)
                serialNumber = parts[2];
        }

        private static void parseUSBDeviceIdCheck(string deviceId)
        {
            if (string.IsNullOrEmpty(deviceId))
                return;

            var parts = deviceId.Split('\\');
            if (parts.Length < 3)
                return;

        }

        private static void disableUSBDeviceWithPowerShell(string vid, string pid)
        {
            try
            {
                string psCommand = $@"
                    $device = Get-PnpDevice | Where-Object {{ $_.InstanceId -like '*USB\VID_{vid}&PID_{pid}*'}}
                    Disable-PnpDevice -InstanceId $device.InstanceId -Confirm:$false";

                ProcessStartInfo psi = new ProcessStartInfo()
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{psCommand}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    Verb = "runas"
                };

                using (Process process = new Process() { StartInfo = psi })
                {
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                DUSBDLogger.WriteLog(ex.Message);
            }
        }
    }
}
