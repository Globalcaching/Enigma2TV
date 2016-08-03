using Enigma2TV.Models;
using Microsoft.Owin.Hosting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            EPGList
        }

        private static MainWindow _instance;
        private string _settingsFolder;
        private Enigma.Enigma2 _enigma;
        private string _currentBouquetSRef;
        private e2servicelist _bougetsServices;
        private e2servicelist _currentBougetService;
        private e2service _currentService;
        private Dictionary<string, e2servicelist> _allBougets = new Dictionary<string, e2servicelist>();
        private DispatcherTimer _channelInfoTimeout;
        private e2settingslist _enigmaSettings;
        private volatile bool msgAck;
        private ViewMode _activeViewMode = ViewMode.TV;
        private DispatcherTimer _epgListRefresh;
        private TimeSpan _timeDiffPCvsEnigma = TimeSpan.FromSeconds(0);

        public event PropertyChangedEventHandler PropertyChanged;

        private List<EPGListEntry> _epgListEntries;
        public List<EPGListEntry> EPGListEntries
        {
            get { return _epgListEntries; }
            set { SetProperty(ref _epgListEntries, value); }
        }


        private EPGListEntry _selectedEPGListEntry;
        public EPGListEntry SelectedEPGListEntry
        {
            get { return _selectedEPGListEntry; }
            set
            {
                SetProperty(ref _selectedEPGListEntry, value);
                if (_selectedEPGListEntry != null)
                {
                    var snow = _enigma.GetEPGSerciceNow(_selectedEPGListEntry.sRef);
                    var snext = _enigma.GetEPGSerciceNext(_selectedEPGListEntry.sRef);

                    SelectedProgramName = snow.e2events[0].e2eventtitle;
                    SelectedProgramInfo = $"{snow.e2events[0].e2eventdescription}\r\n{snow.e2events[0].e2eventdescriptionextended}";
                    SelectedStartTime = _enigma.ConvertDateTime(snow.e2events[0].e2eventstart) ?? DateTime.Now;
                    SelectedEndTime = SelectedStartTime + (_enigma.ConvertTTimeSpan(snow.e2events[0].e2eventduration) ?? TimeSpan.FromSeconds(0));
                    var selectedDurationTime = _enigma.ConvertTTimeSpan(snow.e2events[0].e2eventduration) ?? TimeSpan.FromSeconds(0);
                    var selectedCurrentTime = _enigma.ConvertDateTime(snow.e2events[0].e2eventcurrenttime) ?? DateTime.Now;
                    var selectedRemainingTime = SelectedEndTime - selectedCurrentTime;
                    SelectedProgress = Math.Max(0, 100 - (int)Math.Min((100.0 * selectedRemainingTime.TotalSeconds / selectedDurationTime.TotalSeconds), 100.0));
                    SelectedNextProgramName = snext.e2events[0].e2eventtitle;
                }
            }
        }

        private string _selectedProgramName = "";
        public string SelectedProgramName
        {
            get { return _selectedProgramName; }
            set { SetProperty(ref _selectedProgramName, value); }
        }

        private string _selectedNextProgramName = "";
        public string SelectedNextProgramName
        {
            get { return _selectedNextProgramName; }
            set { SetProperty(ref _selectedNextProgramName, value); }
        }

        private string _selectedProgramInfo = "";
        public string SelectedProgramInfo
        {
            get { return _selectedProgramInfo; }
            set { SetProperty(ref _selectedProgramInfo, value); }
        }

        private DateTime _selectedStartTime = DateTime.Now;
        public DateTime SelectedStartTime
        {
            get { return _selectedStartTime; }
            set { SetProperty(ref _selectedStartTime, value); }
        }

        private DateTime _selectedEndTime = DateTime.Now;
        public DateTime SelectedEndTime
        {
            get { return _selectedEndTime; }
            set { SetProperty(ref _selectedEndTime, value); }
        }

        private int _selectedProgress = 0;
        public int SelectedProgress
        {
            get { return _selectedProgress; }
            set { SetProperty(ref _selectedProgress, value); }
        }


        private Visibility _channelInfoVisibility = Visibility.Hidden;
        public Visibility ChannelInfoVisibility
        {
            get { return _channelInfoVisibility; }
            set { SetProperty(ref _channelInfoVisibility, value); }
        }

        private Visibility _epgListVisibility = Visibility.Collapsed;
        public Visibility EPGListVisibility
        {
            get { return _epgListVisibility; }
            set { SetProperty(ref _epgListVisibility, value); }
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
            EPGListEntries = new List<EPGListEntry>();
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
            if (_epgListRefresh != null)
            {
                _epgListRefresh.Stop();
                _epgListRefresh = null;
            }
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
                    _currentBouquetSRef = (from a in _allBougets.Keys where a == parts[1] select a).FirstOrDefault();
                    if (_currentBouquetSRef != null)
                    {
                        _currentBougetService = _allBougets[_currentBouquetSRef];

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
                                _timeDiffPCvsEnigma = DateTime.Now - (_enigma.ConvertDateTime(curInfo.e2events[0].e2eventcurrenttime) ?? DateTime.Now);
                                ChannelIndex = GetServiceIndexWithinBouquet(_currentService)+1;
                                CurrentChannelName = _currentService.e2servicename;
                                await playM3UFile(_enigma.GetM3UContent(_currentService.e2servicereference));
                                UpdateEPGList();
                                _epgListRefresh = new DispatcherTimer();
                                _epgListRefresh.Interval = TimeSpan.FromSeconds(10);
                                _epgListRefresh.Tick += _epgListRefresh_Tick;
                                _epgListRefresh.Start();
                            }
                        }
                    }
                }
            }
        }

        private void _epgListRefresh_Tick(object sender, EventArgs e)
        {
            CurrentTime = DateTime.Now - _timeDiffPCvsEnigma;
            UpdateEPGListEntriesProgress(true);
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
                CurrentProgress = Math.Max(0, 100-(int)Math.Min((100.0*CurrentRemainingTime.TotalSeconds / CurrentDurationTime.TotalSeconds), 100.0));
            }
        }

        private void UpdateEPGListEntriesProgress(bool checkForRefresh)
        {
            if (EPGListEntries != null)
            {
                foreach (var entry in EPGListEntries)
                {
                    if (entry.Duration.TotalMinutes > 0)
                    {
                        //_timeDiffPCvsEnigma = PC time - Enigma time
                        TimeSpan remaining = (entry.EndTime + _timeDiffPCvsEnigma) - DateTime.Now;
                        entry.Progress = Math.Max(0, 100 - (int)Math.Min((100.0 * remaining.TotalSeconds / entry.Duration.TotalSeconds), 100.0));
                    }
                }
                if (checkForRefresh)
                {
                    foreach (var entry in EPGListEntries)
                    {
                        if (entry.Duration.TotalMinutes > 0 && (entry.EndTime + _timeDiffPCvsEnigma) <= DateTime.Now)
                        {
                            UpdateEPGList();
                            break;
                        }
                    }
                }
            }
        }

        private void UpdateEPGList()
        {
            Task.Factory.StartNew(async () =>
            {
                var epgNow = await _enigma.GetEPGNow(_currentBouquetSRef);
                if (epgNow != null)
                {
                    await Dispatcher.BeginInvoke((Action)(() =>
                    {
                        List<EPGListEntry> epgList;
                        if (EPGListEntries.Count == 0)
                        {
                            EPGListEntries = null;
                            epgList = new List<EPGListEntry>();
                        }
                        else
                        {
                            epgList = EPGListEntries;
                        }
                        for (var i=0; i< epgNow.e2events.Count && i<Settings.Default.MaxEPGBouquetSize; i++)
                        {
                            EPGListEntry entry;
                            if (EPGListEntries==null)
                            {
                                entry = new EPGListEntry();
                                entry.sRef = epgNow.e2events[i].e2eventservicereference;
                                epgList.Add(entry);
                            }
                            else
                            {
                                entry = epgList[i];
                            }
                            entry.ChannelIndex = i + 1;
                            entry.ChannelName = epgNow.e2events[i].e2eventservicename;
                            entry.ProgramName = epgNow.e2events[i].e2eventtitle;
                            entry.StartTime = _enigma.ConvertDateTime(epgNow.e2events[i].e2eventstart) ?? DateTime.Now;
                            entry.Duration = _enigma.ConvertTTimeSpan(epgNow.e2events[i].e2eventduration) ?? TimeSpan.FromSeconds(0);
                            entry.EndTime = entry.StartTime.Add(entry.Duration);
                        }
                        UpdateEPGListEntriesProgress(false);
                        if (EPGListEntries == null)
                        {
                            EPGListEntries = epgList;
                        }
                    }));
                }
            });
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
                case Key.Up:
                case Key.Down:
                    ShowEPGList();
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

        private async Task EPGListInfo_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    if (SelectedEPGListEntry != null && EPGListEntries!= null && EPGListEntries.Count > 0)
                    {
                        var curIndex = EPGListEntries.IndexOf(SelectedEPGListEntry);
                        if (curIndex >= 0)
                        {
                            curIndex--;
                            if (curIndex < 0)
                            {
                                curIndex = EPGListEntries.Count - 1;
                            }
                            SelectedEPGListEntry = EPGListEntries[curIndex];
                            epgList.ScrollIntoView(SelectedEPGListEntry);
                        }
                    }
                    e.Handled = true;
                    break;
                case Key.Left:
                    if (SelectedEPGListEntry != null && EPGListEntries != null && EPGListEntries.Count > 0)
                    {
                        var curIndex = EPGListEntries.IndexOf(SelectedEPGListEntry);
                        if (curIndex >= 0)
                        {
                            curIndex -= (int)(epgList.ActualHeight / epgList.RowHeight);
                            if (curIndex < 0)
                            {
                                curIndex += EPGListEntries.Count;
                            }
                            SelectedEPGListEntry = EPGListEntries[curIndex];
                            epgList.ScrollIntoView(SelectedEPGListEntry);
                        }
                    }
                    e.Handled = true;
                    break;
                case Key.Down:
                    if (SelectedEPGListEntry != null && EPGListEntries != null && EPGListEntries.Count>0)
                    {
                        var curIndex = EPGListEntries.IndexOf(SelectedEPGListEntry);
                        if (curIndex >= 0)
                        {
                            curIndex++;
                            if (curIndex >= EPGListEntries.Count)
                            {
                                curIndex = 0;
                            }
                            SelectedEPGListEntry = EPGListEntries[curIndex];
                            epgList.ScrollIntoView(SelectedEPGListEntry);
                        }
                    }
                    e.Handled = true;
                    break;
                case Key.Right:
                    if (SelectedEPGListEntry != null && EPGListEntries != null && EPGListEntries.Count > 0)
                    {
                        var curIndex = EPGListEntries.IndexOf(SelectedEPGListEntry);
                        if (curIndex >= 0)
                        {
                            curIndex += (int)(epgList.ActualHeight / epgList.RowHeight);
                            if (curIndex >= EPGListEntries.Count)
                            {
                                curIndex = curIndex - EPGListEntries.Count;
                            }
                            SelectedEPGListEntry = EPGListEntries[curIndex];
                            epgList.ScrollIntoView(SelectedEPGListEntry);
                        }
                    }
                    e.Handled = true;
                    break;
                case Key.Enter:
                    if (SelectedEPGListEntry != null)
                    {
                        var selectedService = (from a in _currentBougetService.e2services where a.e2servicereference == SelectedEPGListEntry.sRef select a).FirstOrDefault();
                        if (selectedService != null)
                        {
                            var index = GetServiceIndexWithinBouquet(selectedService);
                            if (index >= 0)
                            {
                                HideEPGList();
                                await ZapToIndex(index);
                            }
                        }
                    }
                    e.Handled = true;
                    break;
                case Key.Escape:
                case Key.X:
                    HideEPGList();
                    e.Handled = true;
                    break;
                default:
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

        private void ShowEPGList()
        {
            HideChannelInfo();
            _activeViewMode = ViewMode.EPGList;
            epgList.Width = 2 * this.ActualWidth / 3;
            epgListInfo.Height = 2 * this.ActualHeight / 4;
            var curIndex = GetServiceIndexWithinBouquet(_currentService);
            EPGListVisibility = Visibility.Visible;
            if (EPGListEntries!=null && curIndex >= 0 && curIndex < EPGListEntries.Count)
            {
                SelectedEPGListEntry = EPGListEntries[curIndex];
                epgList.ScrollIntoView(SelectedEPGListEntry);
            }
            epgList.Focus();
        }

        private void HideEPGList()
        {
            _activeViewMode = ViewMode.TV;
            EPGListVisibility = Visibility.Collapsed;
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

        private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (_activeViewMode)
            {
                case ViewMode.ChannelInfo:
                    ChannelInfo_PreviewKeyDown(sender, e);
                    break;
                case ViewMode.TV:
                    TV_PreviewKeyDown(sender, e);
                    break;
                case ViewMode.EPGList:
                    await EPGListInfo_PreviewKeyDown(sender, e);
                    break;
                default:
                    TV_PreviewKeyDown(sender, e);
                    break;
            }
        }

        private async Task RelocatieTV()
        {
            await StartStream2TV($"pos={(int)this.Left}x{(int)(this.Top+ epgListHeader.ActualHeight)}x{(int)gridTV.ActualWidth}x{(int)gridTV.ActualHeight}");
        }

        private async void gridTV_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            await RelocatieTV();
        }
    }
}
