using Enigma2TV.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Enigma2TV
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AxAXVLC.AxVLCPlugin2 _axWmp;
        private string _settingsFolder;
        private Enigma.Enigma2 _enigma;
        private e2servicelist _bougetsServices;
        private e2servicelist _currentBougetService;
        private e2service _currentService;
        private Dictionary<string, e2servicelist> _allBougets = new Dictionary<string, e2servicelist>();
        private e2settingslist _enigmaSettings;

        public MainWindow()
        {
            InitializeComponent();

            _settingsFolder = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Enigma2TV");
            if (!System.IO.Directory.Exists(_settingsFolder))
            {
                System.IO.Directory.CreateDirectory(_settingsFolder);
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var host = new System.Windows.Forms.Integration.WindowsFormsHost();
            _axWmp = new AxAXVLC.AxVLCPlugin2();
            host.Child = _axWmp;
            this.grid1.Children.Add(host);
            _axWmp.FullscreenEnabled = false;
            _axWmp.Toolbar = false;
            await InitEnigma();
        }

        public async Task InitEnigma()
        {
            _enigma = new Enigma.Enigma2(Settings.Default.IPAddress, Settings.Default.StreamingPort);
            _bougetsServices = await _enigma.GetServices();
            if (_bougetsServices != null)
            {
                _enigmaSettings = await _enigma.GetSettings();
                foreach (var b in _bougetsServices.e2services)
                {
                    _allBougets[b.e2servicereference] = await _enigma.GetServices(b);
                }
                var lastSelectedTVService = (from a in _enigmaSettings.e2settings where a.e2settingname == "config.tv.lastroot" select a.e2settingvalue).FirstOrDefault();
                if (lastSelectedTVService != null)
                {
                    var parts = lastSelectedTVService.Split(';');
                    var s = (from a in _allBougets.Keys where a == parts[1] select a).FirstOrDefault();
                    if (s != null)
                    {
                        _currentBougetService = _allBougets[s];

                        if (_currentBougetService != null)
                        {
                            var powerState = await _enigma.GetPowerState();
                            if (powerState.e2instandby)
                            {
                                await _enigma.ToggleStandby();
                                await Task.Run(() => { System.Threading.Thread.Sleep(1000); });
                            }

                            var curInfo = await _enigma.GetCurrentserviceinformation();
                            if (!string.IsNullOrEmpty(curInfo?.e2service?.e2servicereference))
                            {
                                _currentService = curInfo.e2service;

                                playM3UFile(_enigma.GetM3UContent(_currentService.e2servicereference));
                            }
                        }
                    }
                }
            }
        }

        private void playM3UFile(string content)
        {
            _axWmp.playlist.stop();
            string f = System.IO.Path.Combine(_settingsFolder, "play.m3u");
            System.IO.File.WriteAllText(f, content);
            var uri = new Uri(f);
            var convertedURI = uri.AbsoluteUri;
            _axWmp.playlist.add(convertedURI);
            _axWmp.playlist.play();
            _axWmp.video.deinterlace.disable();
        }

        private async Task ChannelUp()
        {
            var index = GetServiceIndexWithinBouquet(_currentService);
            if (index >= 0)
            {
                index++;
                if (index >= _currentBougetService.e2services.Count)
                {
                    index = 0;
                }
                await ZapToIndex(index);
            }
        }

        private async Task ChannelDown()
        {
            var index = GetServiceIndexWithinBouquet(_currentService);
            if (index >= 0)
            {
                index--;
                if (index < 0)
                {
                    index = _currentBougetService.e2services.Count - 1;
                }
                await ZapToIndex(index);
            }
        }

        private async Task ZapToIndex(int index)
        {
            _axWmp.playlist.stop();
            _axWmp.playlist.items.clear();

            await _enigma.Zap(_currentBougetService.e2services[index]);
            //await Task.Run(() => { System.Threading.Thread.Sleep(1000); });
            var curInfo = await _enigma.GetCurrentserviceinformation();
            _currentService = curInfo.e2service;

            playM3UFile(_enigma.GetM3UContent(_currentService.e2servicereference));
        }

        private int GetServiceIndexWithinBouquet(e2service service)
        {
            int result = -1;
            if (_currentBougetService != null && service!=null)
            {
                for (int i = 0; i < _currentBougetService.e2services.Count; i++)
                {
                    if (_currentBougetService.e2services[i].e2servicereference == service.e2servicereference)
                    {
                        result = i;
                        break;
                    }
                }
            }
            return result;
        }

        private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Right:
                    await ChannelUp();
                    e.Handled = true;
                    break;
                case Key.Left:
                    await ChannelDown();
                    e.Handled = true;
                    break;
                case Key.S:
                    Close();
                    e.Handled = true;
                    break;
            }
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var powerState = await _enigma.GetPowerState();
            if (powerState!=null && !powerState.e2instandby)
            {
                await _enigma.ToggleStandby();
            }
        }
    }
}
