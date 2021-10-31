using OpenUtau.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace OpenUtau.UI.Models
{
    class ProgressBarViewModel : INotifyPropertyChanged, ICmdSubscriber
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        readonly object lockObject = new object();
        Brush _foreground;
        public Brush Foreground { set { _foreground = value; OnPropertyChanged("Foreground"); } get { return _foreground; } }
        public double Progress { set; get; }
        public string Info { set; get; }

        public void Update(ProgressBarNotification cmd)
        {
            lock (lockObject)
            {
                Info = cmd.Info;
                Progress = cmd.Progress;
            }
            OnPropertyChanged("Progress");
            OnPropertyChanged("Info");
        }

        public void OnNext(UCommand cmd, bool isUndo)
        {
            if (cmd is ProgressBarNotification) Update((ProgressBarNotification)cmd);
        }
    }
}
