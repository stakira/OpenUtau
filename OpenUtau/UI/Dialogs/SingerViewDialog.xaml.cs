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
using System.Windows.Shapes;

using OpenUtau.UI.Controls;
using OpenUtau.Core;
using OpenUtau.Core.USTx;

namespace OpenUtau.UI.Dialogs
{
    /// <summary>
    /// Interaction logic for SingerViewDialog.xaml
    /// </summary>
    public partial class SingerViewDialog : Window
    {
        List<string> singerNames;
        public SingerViewDialog()
        {
            InitializeComponent();
            UpdateSingers();
        }

        private void UpdateSingers()
        {
            singerNames = new List<string>();
            foreach (var pair in DocManager.Inst.Singers)
            {
                singerNames.Add(pair.Value.Name);
            }
            if (singerNames.Count > 0)
            {
                this.name.SelectedIndex = 0;
                SetSinger(singerNames[0]);
            }
            this.name.ItemsSource = singerNames;
        }

        public void SetSinger(string singerName)
        {
            USinger singer = null;
            foreach(var pair in DocManager.Inst.Singers)
                if (pair.Value.Name == singerName)
                {
                    singer = pair.Value;
                }
            if (singer == null) return;
            this.name.Text = singer.Name;
            this.avatar.Source = singer.Avatar;
            this.info.Text = "Author: " + singer.Author + "\nWebsite: " + singer.Website + "\nPath: " + singer.Path;
            this.otoview.Items.Clear();
            foreach (var pair in singer.AliasMap) this.otoview.Items.Add(pair.Value);
        }

        private void name_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetSinger(singerNames[this.name.SelectedIndex]);
        }
    }
}
