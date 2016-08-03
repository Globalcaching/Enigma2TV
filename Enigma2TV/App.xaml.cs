using Microsoft.Owin.Hosting;
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
        public static string baseWebAPIAddress = "http://localhost:9091/";

        protected override void OnStartup(StartupEventArgs e)
        {
            WpfSingleInstance.Make("Enigma2TV", this);
            _settings = Settings.Default;
            WebApp.Start<WebApiStartup>(url: baseWebAPIAddress);
            base.OnStartup(e);
        }
    }
}
