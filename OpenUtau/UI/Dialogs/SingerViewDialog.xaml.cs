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
using OpenUtau.Core.USTx;

namespace OpenUtau.UI.Dialogs
{
    /// <summary>
    /// Interaction logic for SingerViewDialog.xaml
    /// </summary>
    public partial class SingerViewDialog : Window
    {
        public SingerViewDialog()
        {
            InitializeComponent();
        }

        public void SetSinger(USinger singer)
        {
            this.Title = this.name.Text = singer.Name;
            this.avatar.Source = singer.Avatar;
            this.info.Text = "Author: " + singer.Author + "\nWebsite: " + singer.Website + "\nPath: " + singer.Path;
            this.otoview.Items.Clear();
            foreach (var pair in singer.AliasMap) this.otoview.Items.Add(pair.Value);
        }
    }
}
