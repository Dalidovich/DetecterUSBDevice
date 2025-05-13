using System.IO;

namespace DetecterUSBDevice
{
    public class DUSBDLogger
    {
        public static void WriteLog(string log,string path= @"C:\DUSBDLog.txt")
        {
            File.AppendAllText(path, $"\n{log}");
        }
    }
}
