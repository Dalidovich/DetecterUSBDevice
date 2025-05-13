using System.Data;
using System.Linq;
using System.Management;

namespace DetecterUSBDevice
{
    public class USBWatcher
    {
        private static USBDeviceDetectorHandler _handler= new USBDeviceDetectorHandler();

        public static void SetWatchers(string[] args)
        {
            ManagementEventWatcher USBConnectWatcher = new ManagementEventWatcher();
            ManagementEventWatcher USBRestoreConnectWatcher = new ManagementEventWatcher();

            var queryConnectUSB = new WqlEventQuery(
            "SELECT * FROM __InstanceCreationEvent WITHIN 2 " +
            "WHERE TargetInstance ISA 'Win32_PnPEntity' " +
            "AND (TargetInstance.DeviceID LIKE 'USB\\\\VID_%')");

            var queryRestoreConnectUSB = new WqlEventQuery(
                "SELECT * FROM __InstanceCreationEvent WITHIN 2 " +
                "WHERE TargetInstance ISA 'Win32_PnPEntity' " +
                "AND (TargetInstance.DeviceID LIKE 'USBSTOR%')");

            var vid = "____";
            var pid = "____";

            if (CheckArgs(args))
            {
                vid = args[0];
                pid = args[1];
            }
            _handler = new USBDeviceDetectorHandler(vid, pid);

            USBConnectWatcher.EventArrived += (sender, e) => _handler.HandleUSBEvent(e, "connect");
            USBRestoreConnectWatcher.EventArrived += (sender, e) => _handler.HandleUSBRestoreEvent(e, "turnOn");
            USBConnectWatcher.Query = queryConnectUSB;
            USBRestoreConnectWatcher.Query = queryRestoreConnectUSB;
            USBConnectWatcher.Start();
            USBRestoreConnectWatcher.Start();
            
        }

        public static bool CheckArgs(string[] args)=> (args.Length == 2 && args.Where(x => x.Length == 4).Count() == 2);

        public static void USBFind()
        {
            DUSBDLogger.WriteLog(_handler._USBDescriptionData);
        }
    }
}
