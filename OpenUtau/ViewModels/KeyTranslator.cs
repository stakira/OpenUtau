using System.Runtime.InteropServices;

namespace OpenUtau.App.ViewModels {
    public static class KeyTranslator {
        private static readonly bool IsMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        /// <summary>
        /// Converts ugly internal Avalonia key names into human-readable strings.
        /// Accounts for macOS and Linux naming conventions.
        /// </summary>
        public static string GetFriendlyName(string keyName) {
            return keyName switch {
                // Modifiers
                "Windows" or "LWin" or "RWin" => IsMac ? "Cmd" : "Win",
                "LeftAlt" or "RightAlt" or "Alt" => IsMac ? "Opt" : "Alt",
                "Control" or "LeftCtrl" or "RightCtrl" or "LControl" or "RControl" => "Ctrl",
                "Shift" or "LeftShift" or "RightShift" => "Shift",
                
                // Navigation & Editing
                "Escape" => "Esc",
                "Return" => "Enter",
                "Back" => IsMac ? "Delete" : "Backspace",
                "Delete" => IsMac ? "Forward Del" : "Del",
                "Insert" => "Ins",
                "PageUp" => "PgUp",
                "PageDown" => "PgDn",
                "Capital" => "Caps Lock",
                "Scroll" => "Scroll Lock",
                "NumLock" => "Num Lock",
                "Snapshot" => "Print Screen",

                // Numpad
                "Divide" => "(Num /)",
                "Multiply" => "(Num *)",
                "Subtract" => "(Num -)",
                "Add" => "(Num +)",
                "Decimal" => "(Num .)",
                "NumPad0" => "Num 0", "NumPad1" => "Num 1", "NumPad2" => "Num 2",
                "NumPad3" => "Num 3", "NumPad4" => "Num 4", "NumPad5" => "Num 5",
                "NumPad6" => "Num 6", "NumPad7" => "Num 7", "NumPad8" => "Num 8",
                "NumPad9" => "Num 9",

                // Digits
                "D1" => "1", "D2" => "2", "D3" => "3", "D4" => "4", "D5" => "5",
                "D6" => "6", "D7" => "7", "D8" => "8", "D9" => "9", "D0" => "0",

                // OEM Symbols
                "OemTilde" or "Oem8" or "Oem3" => "~",
                "OemMinus" or "OemMinusSign" => "-",
                "OemPlus" or "OemPlusSign" => "=",
                "OemOpenBrackets" or "Oem4" => "[",
                "OemCloseBrackets" or "Oem6" => "]",
                "OemPipe" or "Oem5" or "OemBackslash" => "\\",
                "OemSemicolon" or "Oem1" => ";",
                "OemQuotes" or "Oem7" => "'",
                "OemComma" or "OemCommaSign" => ",",
                "OemPeriod" or "OemPeriodSign" => ".",
                "OemQuestion" or "Oem2" => "/",

                _ => keyName
            };
        }
    }
}