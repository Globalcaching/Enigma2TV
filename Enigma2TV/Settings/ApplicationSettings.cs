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
        public string SettingsFolder
        {
            get { return GetProperty(null); }
            set { SetProperty(value); }
        }

        public int SettingsBackupMaxBackups
        {
            get { return int.Parse(GetProperty("3")); }
            set { SetProperty(value.ToString()); }
        }

        public bool SettingsBackupAtStartup
        {
            get { return bool.Parse(GetProperty("True")); }
            set { SetProperty(value.ToString()); }
        }

    }
}
