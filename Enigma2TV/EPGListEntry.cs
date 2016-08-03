using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Enigma2TV
{
    public class EPGListEntry: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string sRef { get; set; }

        private DateTime _startTime = DateTime.Now;
        public DateTime StartTime
        {
            get { return _startTime; }
            set { SetProperty(ref _startTime, value); }
        }

        private DateTime _endTime = DateTime.Now;
        public DateTime EndTime
        {
            get { return _endTime; }
            set { SetProperty(ref _endTime, value); }
        }

        private TimeSpan _duration;
        public TimeSpan Duration
        {
            get { return _duration; }
            set { SetProperty(ref _duration, value); }
        }

        private int _channelIndex = 0;
        public int ChannelIndex
        {
            get { return _channelIndex; }
            set { SetProperty(ref _channelIndex, value); }
        }

        private string _channelName = "";
        public string ChannelName
        {
            get { return _channelName; }
            set { SetProperty(ref _channelName, value); }
        }

        private string _programName = "";
        public string ProgramName
        {
            get { return _programName; }
            set { SetProperty(ref _programName, value); }
        }

        private int _progress = 0;
        public int Progress
        {
            get { return _progress; }
            set { SetProperty(ref _progress, value); }
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
    }
}
