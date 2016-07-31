using Enigma2TV.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
using System.Windows.Threading;

namespace Enigma2TV
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        enum ViewMode
        {
            TV,
            ChannelInfo,
            EPG
        }

        private static MainWindow _instance;
        private string _settingsFolder;
        private Enigma.Enigma2 _enigma;
        private e2servicelist _bougetsServices;
        private e2servicelist _currentBougetService;
        private e2service _currentService;
        private Dictionary<string, e2servicelist> _allBougets = new Dictionary<string, e2servicelist>();
        private DispatcherTimer _channelInfoTimeout;
        private e2settingslist _enigmaSettings;
        private volatile bool msgAck;
        private ViewMode _activeViewMode = ViewMode.TV;

        public event PropertyChangedEventHandler PropertyChanged;

        private Visibility _channelInfoVisibility = Visibility.Hidden;
        public Visibility ChannelInfoVisibility
        {
            get { return _channelInfoVisibility; }
            set { SetProperty(ref _channelInfoVisibility, value); }
        }

        private DateTime _currentTime = DateTime.Now;
        public DateTime CurrentTime
        {
            get { return _currentTime; }
            set { SetProperty(ref _currentTime, value); }
        }

        private int _channelIndex = 0;
        public int ChannelIndex
        {
            get { return _channelIndex; }
            set { SetProperty(ref _channelIndex, value); }
        }

        private string _currentChannelName = "";
        public string CurrentChannelName
        {
            get { return _currentChannelName; }
            set { SetProperty(ref _currentChannelName, value); }
        }

        private DateTime _currentStartTime = DateTime.Now;
        public DateTime CurrentStartTime
        {
            get { return _currentStartTime; }
            set { SetProperty(ref _currentStartTime, value); }
        }

        private string _currentProgramName = "";
        public string CurrentProgramName
        {
            get { return _currentProgramName; }
            set { SetProperty(ref _currentProgramName, value); }
        }

        private int _currentProgress = 0;
        public int CurrentProgress
        {
            get { return _currentProgress; }
            set { SetProperty(ref _currentProgress, value); }
        }

        private TimeSpan _currentRemainingTime;
        public TimeSpan CurrentRemainingTime
        {
            get { return _currentRemainingTime; }
            set { SetProperty(ref _currentRemainingTime, value); }
        }

        private DateTime _nextStartTime = DateTime.Now;
        public DateTime NextStartTime
        {
            get { return _nextStartTime; }
            set { SetProperty(ref _nextStartTime, value); }
        }

        private string _nextProgramName = "";
        public string NextProgramName
        {
            get { return _nextProgramName; }
            set { SetProperty(ref _nextProgramName, value); }
        }

        private TimeSpan _nextDurationTime;
        public TimeSpan NextDurationTime
        {
            get { return _nextDurationTime; }
            set { SetProperty(ref _nextDurationTime, value); }
        }

        public MainWindow()
        {
            _instance = this;
            InitializeComponent();

            _settingsFolder = System.IO.Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Enigma2TV");
            if (!System.IO.Directory.Exists(_settingsFolder))
            {
                System.IO.Directory.CreateDirectory(_settingsFolder);
            }

            _channelInfoTimeout = new DispatcherTimer();
            _channelInfoTimeout.Interval = TimeSpan.FromSeconds(3);
            _channelInfoTimeout.Tick += _channelInfoTimeout_Tick;

            this.DataContext = this;
        }

        public static void processCommandLine(string cmdLine)
        {
            _instance?.ProcessCommandLine(cmdLine);
        }

        public void ProcessCommandLine(string cmdLine)
        {
            if (_instance != null)
            {
                if (cmdLine == "ack")
                {
                    msgAck = true;
                }
            }
        }

        private void _channelInfoTimeout_Tick(object sender, EventArgs e)
        {
            HideChannelInfo();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await StartStream2TV(System.Diagnostics.Process.GetCurrentProcess().Id.ToString());
            await InitEnigma();
            _activeViewMode = ViewMode.TV;
            gridTV.SizeChanged += gridTV_SizeChanged;
            await RelocatieTV();
            this.Activate();
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string name = "")
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                var handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs(name));
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task StartStream2TV(string cmdLine)
        {
            var maxWait = DateTime.Now.AddSeconds(5);
            msgAck = false;
            var s2tv = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "Stream2TV.exe");
            await Task.Run(() =>
            {
                System.Diagnostics.Process.Start(s2tv, cmdLine);
                while (!msgAck && DateTime.Now < maxWait)
                {
                    System.Threading.Thread.Sleep(100);
                }
            });
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
                                ChannelIndex = GetServiceIndexWithinBouquet(_currentService)+1;
                                CurrentChannelName = _currentService.e2servicename;
                                await playM3UFile(_enigma.GetM3UContent(_currentService.e2servicereference));
                            }
                        }
                    }
                }
            }
        }

        private async Task playM3UFile(string content)
        {
            string f = System.IO.Path.Combine(_settingsFolder, "play.m3u");
            System.IO.File.WriteAllText(f, content);
            await StartStream2TV($"m3u=\"{f}\"");
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
            HideChannelInfo();
            ChannelIndex = index+1;
            await StartStream2TV("stop");

            await _enigma.Zap(_currentBougetService.e2services[index]);
            //await Task.Run(() => { System.Threading.Thread.Sleep(1000); });
            await UpdateCurrentChannelInformation();

            await playM3UFile(_enigma.GetM3UContent(_currentService.e2servicereference));
            await ShowChannelInfo(false);
        }

        private async Task UpdateCurrentChannelInformation()
        {
            var curInfo = await _enigma.GetCurrentserviceinformation();
            if (curInfo != null)
            {
                _currentService = curInfo.e2service;
                CurrentChannelName = _currentService.e2servicename;
                CurrentTime = _enigma.ConvertDateTime(curInfo.e2events[0].e2eventcurrenttime) ?? DateTime.Now;
                CurrentStartTime = _enigma.ConvertDateTime(curInfo.e2events[0].e2eventstart) ?? DateTime.Now;
                CurrentProgramName = curInfo.e2events[0].e2eventtitle;
                CurrentRemainingTime = _enigma.ConvertTTimeSpan(curInfo.e2events[0].e2eventremaining) ?? TimeSpan.FromSeconds(0);
                NextStartTime = _enigma.ConvertDateTime(curInfo.e2events[1].e2eventstart) ?? DateTime.Now;
                NextProgramName = curInfo.e2events[1].e2eventtitle;
                NextDurationTime = _enigma.ConvertTTimeSpan(curInfo.e2events[1].e2eventduration) ?? TimeSpan.FromSeconds(0);
                var CurrentDurationTime = _enigma.ConvertTTimeSpan(curInfo.e2events[0].e2eventduration) ?? TimeSpan.FromSeconds(0);
                CurrentProgress = 100-(int)Math.Min((100.0*CurrentRemainingTime.TotalSeconds / CurrentDurationTime.TotalSeconds), 100.0);
            }
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

        private async void TV_PreviewKeyDown(object sender, KeyEventArgs e)
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
                    await CloseApplication();
                    e.Handled = true;
                    break;
                case Key.Enter:
                    await ShowChannelInfo();
                    e.Handled = true;
                    break;
            }
        }

        private void ChannelInfo_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    HideChannelInfo();
                    e.Handled = true;
                    break;
                default:
                    TV_PreviewKeyDown(sender, e);
                    break;
            }
        }

        private async Task ShowChannelInfo(bool getCurrentChannelInfo = true)
        {
            if (_activeViewMode == ViewMode.ChannelInfo || _activeViewMode == ViewMode.TV)
            {
                if (getCurrentChannelInfo)
                {
                    await UpdateCurrentChannelInformation();
                }
                ChannelInfoVisibility = Visibility.Visible;
                _activeViewMode = ViewMode.ChannelInfo;
                _channelInfoTimeout.Start();
            }
        }

        private void HideChannelInfo()
        {
            if (_activeViewMode == ViewMode.ChannelInfo)
            {
                _channelInfoTimeout.Stop();
                ChannelInfoVisibility = Visibility.Hidden;
                _activeViewMode = ViewMode.TV;
            }
        }

        private async Task CloseApplication()
        {
            await StartStream2TV("close");
            _instance = null;
            var powerState = await _enigma.GetPowerState();
            if (powerState != null && !powerState.e2instandby)
            {
                await _enigma.ToggleStandby();
            }
            Close();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (_activeViewMode)
            {
                case ViewMode.ChannelInfo:
                    ChannelInfo_PreviewKeyDown(sender, e);
                    break;
                case ViewMode.TV:
                    TV_PreviewKeyDown(sender, e);
                    break;
                case ViewMode.EPG:
                    break;
                default:
                    TV_PreviewKeyDown(sender, e);
                    break;
            }
        }

        private async Task RelocatieTV()
        {
            await StartStream2TV($"pos={this.Left}x{this.Top}x{gridTV.ActualWidth}x{gridTV.ActualHeight}");
        }

        private async void gridTV_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            await RelocatieTV();
        }
    }
}
