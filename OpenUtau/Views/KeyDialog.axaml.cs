using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ReactiveUI.Fody.Helpers;

using OpenUtau.Core;

namespace OpenUtau.App.Views {
    public partial class KeyDialog : Window {
        static readonly List<string> keyNames = Enumerable.Range(0, 12)
            .Select(i => MusicMath.KeysInOctave[i].Item1)
            .ToList();

        public List<string> KeyNames => keyNames;
        [Reactive] public int Key { get; set; }
        public Action<int>? OnOk { get; set; }
        public KeyDialog() : this(0) { }
        public KeyDialog(int key) {
            InitializeComponent();
            Key=key;
            DataContext = this;
        }
        private void OnOkButtonClick(object sender, RoutedEventArgs args) {
            OnOk?.Invoke(Key);
            Close();
        }
    }
}
