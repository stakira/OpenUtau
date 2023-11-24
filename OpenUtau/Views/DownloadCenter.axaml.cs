using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenUtau.App.ViewModels;
using OpenUtau.Core;

namespace OpenUtau.App.Views {
    public partial class DownloadCenter : Window {
        internal readonly DownloadCenterViewModel ViewModel;

        public DownloadCenter() {
            InitializeComponent();
            DataContext = ViewModel = new DownloadCenterViewModel();
        }
    }

}
