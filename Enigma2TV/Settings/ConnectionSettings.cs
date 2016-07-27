using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Enigma2TV
{
    public partial class Settings
    {
        public string IPAddress
        {
            get { return GetProperty("192.168.2.7"); }
            set { SetProperty(value); }
        }

        public string StreamingPort
        {
            get { return GetProperty("8001"); }
            set { SetProperty(value); }
        }

        public string SelectedBouguet
        {
            get { return GetProperty(""); }
            set { SetProperty(value); }
        }

    }
}
