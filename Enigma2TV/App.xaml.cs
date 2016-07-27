using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Enigma2TV
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        Settings _settings = null;

        protected override void OnStartup(StartupEventArgs e)
        {
            WpfSingleInstance.Make("GAPPSF", this);
            _settings = Settings.Default;

            base.OnStartup(e);
        }
    }
}
