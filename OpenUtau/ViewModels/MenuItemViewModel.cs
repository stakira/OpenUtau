using System.Collections.Generic;
using System.Windows.Input;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using OpenUtau.Core.Ustx;

namespace OpenUtau.App.ViewModels {
    public class MenuItemViewModel {
        public string? Header { get; set; }
        public ICommand? Command { get; set; }
        public object? CommandParameter { get; set; }
        public IList<MenuItemViewModel>? Items { get; set; }
        public double Height { get; set; } = 24;
        public bool IsChecked { get; set; } = false;
        public KeyGesture? InputGesture { get; set; }
    }

    public class SingerMenuItemViewModel : MenuItemViewModel {
        public bool IsFavourite {
            get {
                if(CommandParameter is USinger singer) {
                    return singer.IsFavourite;
                }
                return false;
            }
            set {
                if (CommandParameter is USinger singer) {
                    singer.IsFavourite = value;
                }
            }
        }
        private object? _icon;
        public object? Icon {
            get {
                if(_icon == null) {
                    if (CommandParameter is USinger) {
                        _icon = new ToggleButton() {
                            [!ToggleButton.IsCheckedProperty] = new Binding("IsFavourite")
                        };
                    }
                }
                return _icon;
            }
        }
        public string? Location {
            get {
                if (CommandParameter is USinger singer) {
                    return singer.Location;
                }
                return null;
            }
        }
    }
}
