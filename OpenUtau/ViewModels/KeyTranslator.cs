using System;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Input;
using OpenUtau.Core.Util;

namespace OpenUtau.App.ViewModels {
    public static class KeyTranslator {
        public static readonly bool IsMac = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        /// <summary>
        /// Converts ugly internal Avalonia key names into human-readable strings.
        /// Accounts for macOS and Linux naming conventions.
        /// </summary>
        public static string GetFriendlyName(string keyName) {
            return keyName switch {
                // Modifiers
                "Windows" or "LWin" or "RWin" => IsMac ? "⌘" : "Win",
                "LeftAlt" or "RightAlt" or "Alt" => IsMac ? "⌥" : "Alt",
                "Control" or "LeftCtrl" or "RightCtrl" or "LControl" or "RControl" => IsMac ? "⌘" : "Ctrl",
                "Shift" or "LeftShift" or "RightShift" => IsMac ? "⇧" : "Shift",
                
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

        /// <summary>
        /// Fuzzy matcher for cross-platform keyboard quirks.
        /// If the exact enum doesn't match, it checks known hardware equivalents.
        /// </summary>
        public static bool IsKeyMatch(Key savedKey, Key pressedKey) {
            if (savedKey == pressedKey) return true;

            return savedKey switch {
                Key.OemPipe => pressedKey == Key.Oem5 || pressedKey == Key.OemBackslash,
                Key.OemOpenBrackets => pressedKey == Key.Oem4,
                Key.OemCloseBrackets => pressedKey == Key.Oem6,
                Key.OemQuotes => pressedKey == Key.Oem7,
                Key.OemSemicolon => pressedKey == Key.Oem1,
                Key.OemTilde => pressedKey == Key.Oem3 || pressedKey == Key.Oem8,
                Key.OemMinus => pressedKey == Key.Subtract,
                Key.OemPlus => pressedKey == Key.Add,
                Key.OemQuestion => pressedKey == Key.Oem2,
                _ => false
            };
        }

        /// <summary>
        /// Converts raw Avalonia KeyModifiers into a clean, OS-specific string.
        /// Windows: "Ctrl + Shift" | macOS: "⇧⌘"
        /// </summary>
        public static string GetFriendlyModifiersName(KeyModifiers modifiers) {
            if (modifiers == KeyModifiers.None) return "";
            var parts = new System.Collections.Generic.List<string>();
            if (modifiers.HasFlag(KeyModifiers.Control)) {
                parts.Add(IsMac ? "⌃" : "Ctrl");
            }
            if (modifiers.HasFlag(KeyModifiers.Alt)) {
                parts.Add(IsMac ? "⌥" : "Alt");
            }
            if (modifiers.HasFlag(KeyModifiers.Shift)) {
                parts.Add(IsMac ? "⇧" : "Shift");
            }
            if (modifiers.HasFlag(KeyModifiers.Meta)) {
                parts.Add(IsMac ? "⌘" : "Win");
            }

            // Windows joins with " + ". Mac joins with nothing.
            return string.Join(IsMac ? "" : " + ", parts);
        }

        /// <summary>
        /// Generates a native Avalonia KeyGesture for UI Menus (Top Menu, Right-Click).
        /// Automatically formats for Windows (Ctrl+S) or Mac (⌘S).
        /// </summary>
        public static Avalonia.Input.KeyGesture? GetGesture(string actionId) {
            var sc = Preferences.Default.Shortcuts?.FirstOrDefault(s => s.ActionId == actionId);
            
            if (sc != null && 
                Enum.TryParse<Avalonia.Input.Key>(sc.KeyName, out var k) && 
                Enum.TryParse<Avalonia.Input.KeyModifiers>(sc.ModifiersName, out var m) && 
                k != Avalonia.Input.Key.None) {
                
                return new Avalonia.Input.KeyGesture(k, m);
            }
            return null;
        }
    }
}