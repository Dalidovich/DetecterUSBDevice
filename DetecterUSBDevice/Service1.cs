using System.ServiceProcess;

namespace DetecterUSBDevice
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        protected override async void OnStart(string[] args)
        {
            USBWatcher.SetWatchers(args);
        }
    }
}
