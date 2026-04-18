using System;
using Avalonia;
using Avalonia.Controls;
using OpenUtau.App.Controls;
using OpenUtau.Core.Util;

namespace OpenUtau.App.Views {
    public partial class PianoRollDetachedWindow : Window {
        private readonly PianoRoll pianoRoll;
        private bool forceClose;

        public PianoRollDetachedWindow(PianoRoll pianoRoll) {
            InitializeComponent();
            this.pianoRoll = pianoRoll;
            DataContext = pianoRoll.DataContext;

            PianoRollContainer.Content = pianoRoll;

            if (Preferences.Default.PianorollWindowSize.TryGetPosition(out int x, out int y)) {
                Position = new PixelPoint(x, y);
            }
            WindowState = (WindowState)Preferences.Default.PianorollWindowSize.State;
        }

        public void WindowClosing(object? sender, WindowClosingEventArgs e) {
            // Check window state first cuz it flops sometimes
            if (WindowState != WindowState.Minimized) {
                Preferences.Default.PianorollWindowSize.Set(Width, Height, Position.X, Position.Y, (int)WindowState);
            } else {
                // when the window flops like maximized -> minimized, the size and position are lost
                // so we need to restore them to the default values
                var prevSize = Preferences.Default.PianorollWindowSize;
                Preferences.Default.PianorollWindowSize.Set(
                    prevSize.Width, 
                    prevSize.Height, 
                    prevSize.PositionX ?? 0, 
                    prevSize.PositionY ?? 0, 
                    (int)WindowState.Normal 
                );
            }
            Preferences.Save();
            Hide();
            e.Cancel = !forceClose;
        }

        public void WindowDeactivated(object sender, EventArgs args) {
            pianoRoll.LyricBox?.EndEdit();
        }

        public void ForceClose() {
            PianoRollContainer.Content = null;
            forceClose = true;
            Close();
        }

    }
}
