using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Stream2TV
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.Integration.WindowsFormsHost _vlcHost;
        private AxAXVLC.AxVLCPlugin2 _axWmp;
        private static MainWindow _instance;
        private DispatcherTimer _checkAliveTimer;
        private int _parentProcessId = -1;

        public MainWindow()
        {
            var args = Environment.GetCommandLineArgs();
            if (args != null && args.Length > 0)
            {
                string pid = args[args.Length - 1];
                int.TryParse(pid, out _parentProcessId);
            }
            _instance = this;
            InitializeComponent();
            _checkAliveTimer = new DispatcherTimer();
            _checkAliveTimer.Interval = TimeSpan.FromSeconds(2);
            _checkAliveTimer.Tick += _checkAliveTimer_Tick;
            _checkAliveTimer.Start();
        }

        private void _checkAliveTimer_Tick(object sender, EventArgs e)
        {
            if (System.Diagnostics.Process.GetProcessById(_parentProcessId)==null)
            {
                //Dispatcher.BeginInvoke((Action)(() => 
                //{ 
                    CloseApp();
                //}));
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _vlcHost = new System.Windows.Forms.Integration.WindowsFormsHost();
            _axWmp = new AxAXVLC.AxVLCPlugin2();
            _vlcHost.Child = _axWmp;
            this.gridTV.Children.Add(_vlcHost);
            _axWmp.FullscreenEnabled = false;
            _axWmp.Toolbar = false;
        }

        public static void processCommandLine(string cmdLine)
        {
            _instance?.ProcessCommandLine(cmdLine);
        }

        private void AckMessage()
        {
            var e2tv = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "Enigma2TV.exe");
            System.Diagnostics.Process.Start(e2tv, "ack");
        }

        private void ProcessCommandLine(string cmdLine)
        {
            if (_instance != null)
            {
                Dispatcher.BeginInvoke((Action)(() =>
                {
                    if (_instance != null)
                    {
                        if (cmdLine.StartsWith("close"))
                        {
                            CloseApp();
                        }
                        else if (cmdLine.StartsWith("m3u="))
                        {
                            playM3UFile(cmdLine.Substring(4).Replace("\"", ""));
                            AckMessage();
                        }
                        else if (cmdLine.StartsWith("pos="))
                        {
                            var parts = cmdLine.Substring(4).Split('x');
                            SetBounds(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]), int.Parse(parts[3]));
                            AckMessage();
                        }
                        else if (cmdLine.StartsWith("stop"))
                        {
                            _axWmp.playlist.stop();
                            _axWmp.playlist.items.clear();
                            AckMessage();
                        }
                    }
                }));
            }
        }

        private void CloseApp()
        {
            if (_instance != null)
            {
                _checkAliveTimer.Stop();
                _instance = null;
                _axWmp.playlist.stop();
                _axWmp.playlist.items.clear();
                System.Threading.Thread.Sleep(1000);
                Close();
            }
        }

        private void playM3UFile(string fn)
        {
            _axWmp.playlist.stop();
            var uri = new Uri(fn);
            var convertedURI = uri.AbsoluteUri;
            _axWmp.playlist.add(convertedURI);
            _axWmp.playlist.play();
            _axWmp.video.deinterlace.disable();
        }

        private IntPtr _handle;
        private void SetBounds(int left, int top, int width, int height)
        {
            if (_handle == IntPtr.Zero)
                _handle = new WindowInteropHelper(this).Handle;

            SetWindowPos(_handle, IntPtr.Zero, left, top, width, height, 0);
        }

        [DllImport("user32")]
        static extern bool SetWindowPos(
                IntPtr hWnd,
                IntPtr hWndInsertAfter,
                int x,
                int y,
                int cx,
                int cy,
                uint uFlags);
    }
}
