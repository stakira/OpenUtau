using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using OpenUtau.Audio;
using OpenUtau.Classic;
using OpenUtau.Core;
using OpenUtau.Core.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using OpenUtau.Core.Render;
using Serilog;
using Avalonia.Input;
using System.Collections.ObjectModel;
using System.Reactive;
using OpenUtau.Core.Editing;

namespace OpenUtau.App.ViewModels {
    public class LyricsHelperOption {
        public readonly Type klass;
        public LyricsHelperOption(Type klass) {
            this.klass = klass;
        }
        public override string ToString() {
            return klass.Name;
        }
    }
    public class ShortcutsRefreshEvent { }
    public class ShortcutItemViewModel : ViewModelBase {
        public string ActionName { get; set; } = string.Empty;
        public string ActionId { get; set; } = string.Empty;
        
        [Reactive] public Key Key { get; set; }
        [Reactive] public KeyModifiers Modifiers { get; set; }
        [Reactive] public bool IsListening { get; set; }

        public string DisplayString {
            get {
                if (IsListening) return ThemeManager.GetString("prefs.shortcuts.listening") ?? "Press keys...";
                
                string mods = Modifiers == KeyModifiers.None ? "" : $"{Modifiers} + ";
                string friendlyKey = KeyTranslator.GetFriendlyName(Key.ToString());
                return $"{mods}{friendlyKey}";
            }
        }

        public void RefreshDisplay() {
            this.RaisePropertyChanged(nameof(DisplayString));
        }
    }
    public class PreferencesViewModel : ViewModelBase {
        // General
        private CultureInfo? language;
        private CultureInfo? sortingOrder;

        public List<CultureInfo>? Languages { get; }
        public CultureInfo? Language {
            get => language;
            set => this.RaiseAndSetIfChanged(ref language, value);
        }
        public List<CultureInfo>? SortingOrders { get; }
        public CultureInfo? SortingOrder {
            get => sortingOrder;
            set => this.RaiseAndSetIfChanged(ref sortingOrder, value);
        }
        [Reactive] public bool Beta { get; set; }

        // Playback
        private List<AudioOutputDevice>? audioOutputDevices;
        private AudioOutputDevice? audioOutputDevice;

        public List<AudioOutputDevice>? AudioOutputDevices {
            get => audioOutputDevices;
            set => this.RaiseAndSetIfChanged(ref audioOutputDevices, value);
        }
        public AudioOutputDevice? AudioOutputDevice {
            get => audioOutputDevice;
            set => this.RaiseAndSetIfChanged(ref audioOutputDevice, value);
        }
        [Reactive] public bool UseSystemDefaultDevice { get; set; }
        [Reactive] public int PreferPortAudio { get; set; }
        [Reactive] public int LockStartTime { get; set; }
        [Reactive] public int PlaybackAutoScroll { get; set; }
        [Reactive] public double PlayPosMarkerMargin { get; set; }

        // Paths
        public string SingerPath => PathManager.Inst.SingersPath;
        public string AdditionalSingersPath => !string.IsNullOrWhiteSpace(PathManager.Inst.AdditionalSingersPath) ? PathManager.Inst.AdditionalSingersPath : "(None)";
        [Reactive] public bool InstallToAdditionalSingersPath { get; set; }
        [Reactive] public bool LoadDeepFolders { get; set; }

        // Editing
        public List<LyricsHelperOption> LyricsHelpers { get; } =
            ActiveLyricsHelper.Inst.Available
                .Select(klass => new LyricsHelperOption(klass))
                .ToList();
        [Reactive] public LyricsHelperOption? LyricsHelper { get; set; }
        [Reactive] public bool LyricsHelperBrackets { get; set; }
        [Reactive] public bool PenPlusDefault { get; set; }

        // Render
        [Reactive] public bool PreRender { get; set; }
        [Reactive] public int NumRenderThreads { get; set; }
        public int LogicalCoreCount {
            get => Environment.ProcessorCount;
        }
        [Reactive] public bool HighThreads { get; set; }
        public int SafeMaxThreadCount {
            get => Math.Min(8, LogicalCoreCount / 2);
        }
        [Reactive] public bool SkipRenderingMutedTracks { get; set; }
        [Reactive] public bool ClearCacheOnQuit { get; set; }
        public List<string> OnnxRunnerOptions { get; set; }
        [Reactive] public string OnnxRunner { get; set; }
        public List<GpuInfo> OnnxGpuOptions { get; set; }
        [Reactive] public GpuInfo OnnxGpu { get; set; }
        [Reactive] public bool ShowOnnxGpu { get; set; }

        // Appearance
        [Reactive] public string ThemeName { get; set; }
        [Reactive] public int DegreeStyle { get; set; }
        [Reactive] public bool UseTrackColor { get; set; }
        [Reactive] public bool ShowPortrait { get; set; }
        [Reactive] public bool ShowIcon { get; set; }
        [Reactive] public bool ShowGhostNotes { get; set; }
        [Reactive] public bool ThemeEditable { get; set; }
        public List<string> ThemeItems => ThemeManager.GetAvailableThemes();
        public bool IsThemeEditorOpen => Views.ThemeEditorWindow.IsOpen;

        // UTAU
        public List<string> DefaultRendererOptions { get; set; }
        [Reactive] public string DefaultRenderer { get; set; }
        [Reactive] public int OtoEditor { get; set; }
        public string VLabelerPath => Preferences.Default.VLabelerPath;
        public string SetParamPath => Preferences.Default.SetParamPath;

        // Diffsinger
        public List<int> DiffSingerStepsOptions { get; } = new List<int> { 2, 5, 10, 20, 50, 100, 200, 500, 1000 };
        public List<int> DiffSingerStepsVarianceOptions { get; } = new List<int> { 2, 5, 10, 20, 50, 100, 200, 500, 1000 };
        public List<int> DiffSingerStepsPitchOptions { get; } = new List<int> { 2, 5, 10, 20, 50, 100, 200, 500, 1000 };
        [Reactive] public int DiffSingerSteps { get; set; }
        [Reactive] public int DiffSingerStepsVariance { get; set; }
        [Reactive] public int DiffSingerStepsPitch { get; set; }
        [Reactive] public double DiffSingerDepth { get; set; }
        [Reactive] public bool DiffSingerTensorCache { get; set; }
        [Reactive] public bool DiffSingerLangCodeHide { get; set; }

        // Advanced
        [Reactive] public bool RememberMid { get; set; }
        [Reactive] public bool RememberUst { get; set; }
        [Reactive] public bool RememberVsqx { get; set; }
        public string WinePath => Preferences.Default.WinePath;

        // Shortcuts
        [Reactive] public ShortcutItemViewModel? ActiveShortcut { get; set; }
        public void ListenForShortcut(ShortcutItemViewModel item) {
            // Cancel any existing listening item
            if (ActiveShortcut != null) {
                ActiveShortcut.IsListening = false;
                ActiveShortcut.RefreshDisplay();
            }

            ActiveShortcut = item;
            ActiveShortcut.IsListening = true;
            ActiveShortcut.RefreshDisplay();
        }

        private void SaveShortcuts() {
            Preferences.Default.Shortcuts.Clear();
            foreach (var sc in allShortcuts) { 
                Preferences.Default.Shortcuts.Add(new Preferences.ShortcutBinding {
                    ActionId = sc.ActionId,
                    KeyName = sc.Key.ToString(),
                    ModifiersName = sc.Modifiers.ToString()
                });
            }
            Preferences.Save();
            MessageBus.Current.SendMessage(new ShortcutsRefreshEvent());
        }

        public void AssignShortcut(Key key, KeyModifiers modifiers) {
            if (ActiveShortcut == null) return;
            if (key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftShift || key == Key.RightShift || key == Key.LeftAlt || key == Key.RightAlt || key == Key.LWin || key == Key.RWin) {
                return;
            }

            var duplicate = allShortcuts.FirstOrDefault(s => s != ActiveShortcut && s.Key == key && s.Modifiers == modifiers);
            
            if (duplicate != null) {
                ActiveShortcut.IsListening = false;
                ActiveShortcut.RefreshDisplay();
                ActiveShortcut = null;
                string formatString = ThemeManager.GetString("prefs.shortcuts.duplicate") ?? "The shortcut '{0}' is already assigned to '{1}'.";
                string message = string.Format(formatString, duplicate.DisplayString, duplicate.ActionName);
                DocManager.Inst.ExecuteCmd(new ErrorMessageNotification(message));
                return; 
            }
            ActiveShortcut.Key = key;
            ActiveShortcut.Modifiers = modifiers;
            ActiveShortcut.IsListening = false;
            ActiveShortcut.RefreshDisplay();
            ActiveShortcut = null;
            SaveShortcuts();
        }

        public void ResetShortcut(ShortcutItemViewModel item) {
            var defaults = new Preferences.SerializablePreferences().Shortcuts;
            var defaultBinding = defaults.FirstOrDefault(s => s.ActionId == item.ActionId);
            
            if (defaultBinding != null && 
                Enum.TryParse<Key>(defaultBinding.KeyName, out var defKey) && 
                Enum.TryParse<KeyModifiers>(defaultBinding.ModifiersName, out var defMods)) {
                
                item.Key = defKey;
                item.Modifiers = defMods;
            } else {
                item.Key = Key.None;
                item.Modifiers = KeyModifiers.None;
            }
            item.IsListening = false;
            item.RefreshDisplay();
            SaveShortcuts();
        }

        public void ResetAllShortcuts() {
            var defaults = new Preferences.SerializablePreferences().Shortcuts;

            Preferences.Default.Shortcuts = defaults.ToList(); 
            Preferences.Save(); 
            foreach (var item in allShortcuts) {
                var defaultBinding = defaults.FirstOrDefault(s => s.ActionId == item.ActionId);
                
                if (defaultBinding != null && 
                    Enum.TryParse<Key>(defaultBinding.KeyName, out var defKey) && 
                    Enum.TryParse<KeyModifiers>(defaultBinding.ModifiersName, out var defMods)) {
                    
                    item.Key = defKey;
                    item.Modifiers = defMods;
                } else {
                    item.Key = Key.None;
                    item.Modifiers = KeyModifiers.None;
                }
                
                item.IsListening = false;
                item.RefreshDisplay();
            }
            SaveShortcuts(); 
        }
        private List<ShortcutItemViewModel> allShortcuts = new List<ShortcutItemViewModel>();
        public ObservableCollection<ShortcutItemViewModel> FilteredShortcuts { get; } = new ObservableCollection<ShortcutItemViewModel>();
        [Reactive] public string ShortcutSearchText { get; set; } = string.Empty;
        public ReactiveCommand<ShortcutItemViewModel, Unit> ListenForShortcutCommand { get; }

        public PreferencesViewModel() {
            ListenForShortcutCommand = ReactiveCommand.Create<ShortcutItemViewModel>(ListenForShortcut);

            var validActionIds = new HashSet<string>();
            
            var defaultShortcuts = new Preferences.SerializablePreferences().Shortcuts;
            foreach (var sc in defaultShortcuts) {
                validActionIds.Add(sc.ActionId);
            }
            foreach (var type in DocManager.Inst.ExternalBatchEditTypes) {
                try { if (Activator.CreateInstance(type) is BatchEdit edit) validActionIds.Add(edit.Name); } catch { }
            }
            if (DocManager.Inst.Plugins != null) {
                foreach (var plugin in DocManager.Inst.Plugins) {
                    validActionIds.Add(plugin.Name);
                }
            }
            if (Preferences.Default.Shortcuts != null) {
                var orderedShortcuts = new List<Preferences.ShortcutBinding>();
                bool requiresSave = false;

                foreach (var defaultBinding in defaultShortcuts) {
                    var userBinding = Preferences.Default.Shortcuts.FirstOrDefault(s => s.ActionId == defaultBinding.ActionId);

                    if (userBinding != null) {
                        orderedShortcuts.Add(userBinding);
                    } else {
                        orderedShortcuts.Add(new Preferences.ShortcutBinding {
                            ActionId = defaultBinding.ActionId,
                            KeyName = defaultBinding.KeyName,
                            ModifiersName = defaultBinding.ModifiersName
                        });
                        requiresSave = true;
                    }
                }

                foreach (var userBinding in Preferences.Default.Shortcuts) {
                    bool isDefault = defaultShortcuts.Any(d => d.ActionId == userBinding.ActionId);
                    bool isValidPlugin = validActionIds.Contains(userBinding.ActionId);

                    if (!isDefault && isValidPlugin) {
                        orderedShortcuts.Add(userBinding);
                    }
                }

                if (Preferences.Default.Shortcuts.Count != orderedShortcuts.Count) {
                    requiresSave = true;
                } else {
                    for (int i = 0; i < orderedShortcuts.Count; i++) {
                        if (Preferences.Default.Shortcuts[i].ActionId != orderedShortcuts[i].ActionId) {
                            requiresSave = true;
                            break;
                        }
                    }
                }

                if (requiresSave) {
                    Preferences.Default.Shortcuts = orderedShortcuts;
                    Preferences.Save();
                }
            }

            if (Preferences.Default.Shortcuts != null) {
                foreach (var binding in Preferences.Default.Shortcuts) {
                    
                    Key parsedKey = Key.None;
                    KeyModifiers parsedMods = KeyModifiers.None;
                    
                    if (!string.IsNullOrEmpty(binding.KeyName)) {
                        Enum.TryParse(binding.KeyName, out parsedKey);
                    }
                    if (!string.IsNullOrEmpty(binding.ModifiersName)) {
                        Enum.TryParse(binding.ModifiersName, out parsedMods);
                    }

                    string lookupKey = "shortcut." + binding.ActionId;
                    string displayName = ThemeManager.GetString(lookupKey);
                    
                    if (string.IsNullOrEmpty(displayName) || displayName == lookupKey) {
                        displayName = ThemeManager.GetString(binding.ActionId);
                    }

                    if (string.IsNullOrEmpty(displayName) || displayName == binding.ActionId) {
                        displayName = binding.ActionId;
                    }

                    if (displayName.StartsWith("shortcut.")) {
                        displayName = displayName.Substring(9);
                    }

                    allShortcuts.Add(new ShortcutItemViewModel {
                        ActionId = binding.ActionId,
                        ActionName = displayName,
                        Key = parsedKey,
                        Modifiers = parsedMods
                    });
                }
            }
            
            // external batch edits
            foreach (var type in DocManager.Inst.ExternalBatchEditTypes) {
                try {
                    if (Activator.CreateInstance(type) is BatchEdit edit) {
                        
                        var savedSc = Preferences.Default.Shortcuts?.FirstOrDefault(s => s.ActionId == edit.Name);
                        Key savedKey = Key.None;
                        KeyModifiers savedMods = KeyModifiers.None;
                        
                        if (savedSc != null) {
                            Enum.TryParse(savedSc.KeyName, out savedKey);
                            Enum.TryParse(savedSc.ModifiersName, out savedMods);
                        }

                        string pluginName = edit.Name;
                        if (allShortcuts.Any(s => s.ActionId == pluginName)) {
                            continue; 
                        }
                        
                        string lookupKey = "shortcut." + pluginName;
                        string displayName = ThemeManager.GetString(lookupKey);
                        
                        if (string.IsNullOrEmpty(displayName) || displayName == lookupKey) {
                            displayName = pluginName;
                        }
                        if (displayName.StartsWith("shortcut.")) {
                            displayName = displayName.Substring(9);
                        }

                        allShortcuts.Add(new ShortcutItemViewModel {
                            ActionId = pluginName, 
                            ActionName = displayName,
                            Key = savedKey,
                            Modifiers = savedMods
                        });
                    }
                } catch { 
                }
            }
            
            // legacy plugins
            if (DocManager.Inst.Plugins != null) {
                foreach (var plugin in DocManager.Inst.Plugins) {
                    try {
                        var savedSc = Preferences.Default.Shortcuts?.FirstOrDefault(s => s.ActionId == plugin.Name);
                        
                        Key savedKey = Key.None;
                        KeyModifiers savedMods = KeyModifiers.None;
                        
                        if (savedSc != null) {
                            Enum.TryParse(savedSc.KeyName, out savedKey);
                            Enum.TryParse(savedSc.ModifiersName, out savedMods);
                        }

                        string pluginName = plugin.Name;
                        string lookupKey = "shortcut." + pluginName;
                        string displayName = ThemeManager.GetString(lookupKey);
                        
                        if (string.IsNullOrEmpty(displayName) || displayName == lookupKey) {
                            displayName = pluginName;
                        }

                        if (displayName.StartsWith("shortcut.")) {
                            displayName = displayName.Substring(9);
                        }

                        string legacyTag = ThemeManager.GetString("pianoroll.menu.part.legacypluginexp.shortcuts");
                        if (string.IsNullOrEmpty(legacyTag) || legacyTag == "pianoroll.menu.part.legacypluginexp.shortcuts") {
                            legacyTag = "(legacy)";
                        }

                        var existingItem = allShortcuts.FirstOrDefault(s => s.ActionId == pluginName);
                        if (existingItem != null) {
                            if (!existingItem.ActionName.EndsWith(legacyTag)) {
                                existingItem.ActionName = $"{existingItem.ActionName} {legacyTag}";
                            }
                            continue;
                        }

                        allShortcuts.Add(new ShortcutItemViewModel {
                            ActionId = pluginName, 
                            ActionName = $"{displayName} {legacyTag}",
                            Key = savedKey,
                            Modifiers = savedMods
                        });
                    } catch {
                        
                    }
                }
            }

            var uniqueShortcuts = allShortcuts
                .GroupBy(sc => sc.ActionId)
                .Select(group => group.First())
                .ToList();

            allShortcuts.Clear();
            foreach (var sc in uniqueShortcuts) {
                allShortcuts.Add(sc);
            }

            this.WhenAnyValue(vm => vm.ShortcutSearchText)
            .Subscribe(text => {
                FilteredShortcuts.Clear();
                var lowerText = text?.ToLowerInvariant() ?? string.Empty;
                foreach (var sc in allShortcuts) {
                    if (string.IsNullOrEmpty(lowerText) || 
                        sc.ActionName.ToLowerInvariant().Contains(lowerText) || 
                        sc.DisplayString.ToLowerInvariant().Contains(lowerText)) {
                        FilteredShortcuts.Add(sc);
                    }
                }
            });

            var audioOutput = PlaybackManager.Inst.AudioOutput;
            if (audioOutput != null) {
                AudioOutputDevices = audioOutput.GetOutputDevices();
                int deviceNumber = audioOutput.DeviceNumber;
                var device = AudioOutputDevices.FirstOrDefault(d => d.deviceNumber == deviceNumber);
                if (device != null) {
                    AudioOutputDevice = device;
                }
            }
            UseSystemDefaultDevice = Preferences.Default.UseSystemDefaultAudioDevice;
            PreferPortAudio = Preferences.Default.PreferPortAudio ? 1 : 0;
            PlaybackAutoScroll = Preferences.Default.PlaybackAutoScroll;
            PlayPosMarkerMargin = Preferences.Default.PlayPosMarkerMargin;
            LockStartTime = Preferences.Default.LockStartTime;
            InstallToAdditionalSingersPath = Preferences.Default.InstallToAdditionalSingersPath;
            LoadDeepFolders = Preferences.Default.LoadDeepFolderSinger;
            ToolsManager.Inst.Initialize();
            var pattern = new Regex(@"Strings\.([\w-]+)\.axaml");
            Languages = App.GetLanguages().Keys
                .Select(lang => CultureInfo.GetCultureInfo(lang))
                .ToList();
            Language = string.IsNullOrEmpty(Preferences.Default.Language)
                ? null
                : CultureInfo.GetCultureInfo(Preferences.Default.Language);
            SortingOrders = Languages.ToList();
            SortingOrders.Insert(0, CultureInfo.InvariantCulture);
            SortingOrder = Preferences.Default.SortingOrder == null ? Language
                : string.IsNullOrEmpty(Preferences.Default.SortingOrder) ? CultureInfo.InvariantCulture
                : CultureInfo.GetCultureInfo(Preferences.Default.SortingOrder);
            PreRender = Preferences.Default.PreRender;
            DefaultRendererOptions = Renderers.getRendererOptions();
            DefaultRenderer = String.IsNullOrEmpty(Preferences.Default.DefaultRenderer) ?
               DefaultRendererOptions[0] : Preferences.Default.DefaultRenderer;
            NumRenderThreads = Preferences.Default.NumRenderThreads;
            OnnxRunnerOptions = Onnx.getRunnerOptions();
            OnnxRunner = String.IsNullOrEmpty(Preferences.Default.OnnxRunner) ?
               OnnxRunnerOptions[0] : Preferences.Default.OnnxRunner;
            OnnxGpuOptions = Onnx.getGpuInfo();
            OnnxGpu = OnnxGpuOptions.FirstOrDefault(x => x.deviceId == Preferences.Default.OnnxGpu, OnnxGpuOptions[0]);
            ShowOnnxGpu = OnnxRunner == "DirectML";
            DiffSingerDepth = Preferences.Default.DiffSingerDepth * 100;
            DiffSingerSteps = Preferences.Default.DiffSingerSteps;
            DiffSingerStepsVariance = Preferences.Default.DiffSingerStepsVariance;
            DiffSingerStepsPitch = Preferences.Default.DiffSingerStepsPitch;
            DiffSingerTensorCache = Preferences.Default.DiffSingerTensorCache;
            DiffSingerLangCodeHide = Preferences.Default.DiffSingerLangCodeHide;
            SkipRenderingMutedTracks = Preferences.Default.SkipRenderingMutedTracks;
            ThemeName = Preferences.Default.ThemeName;
            PenPlusDefault = Preferences.Default.PenPlusDefault;
            DegreeStyle = Preferences.Default.DegreeStyle;
            UseTrackColor = Preferences.Default.UseTrackColor;
            ShowPortrait = Preferences.Default.ShowPortrait;
            ShowIcon = Preferences.Default.ShowIcon;
            ShowGhostNotes = Preferences.Default.ShowGhostNotes;
            Beta = Preferences.Default.Beta;
            LyricsHelper = LyricsHelpers.FirstOrDefault(option => option.klass.Equals(ActiveLyricsHelper.Inst.GetPreferred()));
            LyricsHelperBrackets = Preferences.Default.LyricsHelperBrackets;
            OtoEditor = Preferences.Default.OtoEditor;
            RememberMid = Preferences.Default.RememberMid;
            RememberUst = Preferences.Default.RememberUst;
            RememberVsqx = Preferences.Default.RememberVsqx;
            ClearCacheOnQuit = Preferences.Default.ClearCacheOnQuit;

            MessageBus.Current.Listen<ThemeEditorStateChangedEvent>()
                .Subscribe(_ => this.RaisePropertyChanged(nameof(IsThemeEditorOpen)));
            
            this.WhenAnyValue(vm => vm.UseSystemDefaultDevice)
                .Subscribe(useDefault => {
                    Preferences.Default.UseSystemDefaultAudioDevice = useDefault;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.AudioOutputDevice)
                .WhereNotNull()
                .SubscribeOn(RxApp.MainThreadScheduler)
                .Subscribe(device => {
                    if (UseSystemDefaultDevice) {
                        return;
                    }
                    if (PlaybackManager.Inst.AudioOutput != null) {
                        try {
                            PlaybackManager.Inst.AudioOutput.SelectDevice(device.guid, device.deviceNumber);
                        } catch (Exception e) {
                            DocManager.Inst.ExecuteCmd(new ErrorMessageNotification($"Failed to select device {device.name}", e));
                        }
                    }
                });
            this.WhenAnyValue(vm => vm.PreferPortAudio)
                .Subscribe(index => {
                    Preferences.Default.PreferPortAudio = index > 0;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.PlaybackAutoScroll)
                .Subscribe(autoScroll => {
                    Preferences.Default.PlaybackAutoScroll = autoScroll;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.PlayPosMarkerMargin)
                .Subscribe(playPosMarkerMargin => {
                    Preferences.Default.PlayPosMarkerMargin = playPosMarkerMargin;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.LockStartTime)
                .Subscribe(lockStartTime => {
                    Preferences.Default.LockStartTime = lockStartTime;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.InstallToAdditionalSingersPath)
                .Subscribe(additionalSingersPath => {
                    Preferences.Default.InstallToAdditionalSingersPath = additionalSingersPath;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.LoadDeepFolders)
                .Subscribe(loadDeepFolders => {
                    Preferences.Default.LoadDeepFolderSinger = loadDeepFolders;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.PreRender)
                .Subscribe(preRender => {
                    Preferences.Default.PreRender = preRender;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.PenPlusDefault)
                .Subscribe(penPlusDefault => {
                    Preferences.Default.PenPlusDefault = penPlusDefault;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.Language)
                .Subscribe(lang => {
                    Preferences.Default.Language = lang?.Name ?? string.Empty;
                    Preferences.Save();
                    App.SetLanguage(Preferences.Default.Language);
                });
            this.WhenAnyValue(vm => vm.SortingOrder)
                .Subscribe(so => {
                    Preferences.Default.SortingOrder = so?.Name ?? null;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.ThemeName)
                .Subscribe(themeName => {
                    ThemeEditable = themeName != "Light" && themeName != "Dark";
                    if (!IsThemeEditorOpen) {
                        Preferences.Default.ThemeName = themeName;
                        Preferences.Save();
                        App.SetTheme();
                    }
                });
            this.WhenAnyValue(vm => vm.DegreeStyle)
                .Subscribe(degreeStyle => {
                    Preferences.Default.DegreeStyle = degreeStyle;
                    Preferences.Save();
                    MessageBus.Current.SendMessage(new PianorollRefreshEvent("Part"));
                });
            this.WhenAnyValue(vm => vm.UseTrackColor)
                .Subscribe(trackColor => {
                    Preferences.Default.UseTrackColor = trackColor;
                    Preferences.Save();
                    MessageBus.Current.SendMessage(new PianorollRefreshEvent("TrackColor"));
                });
            this.WhenAnyValue(vm => vm.ShowPortrait)
                .Subscribe(showPortrait => {
                    Preferences.Default.ShowPortrait = showPortrait;
                    Preferences.Save();
                    MessageBus.Current.SendMessage(new PianorollRefreshEvent("Portrait"));
                });
            this.WhenAnyValue(vm => vm.ShowIcon)
                .Subscribe(showIcon => {
                    Preferences.Default.ShowIcon = showIcon;
                    Preferences.Save();
                    MessageBus.Current.SendMessage(new PianorollRefreshEvent("Portrait"));
                });
            this.WhenAnyValue(vm => vm.ShowGhostNotes)
                .Subscribe(showGhostNotes => {
                    Preferences.Default.ShowGhostNotes = showGhostNotes;
                    Preferences.Save();
                    MessageBus.Current.SendMessage(new PianorollRefreshEvent("Part"));
                });
            this.WhenAnyValue(vm => vm.Beta)
                .Subscribe(beta => {
                    Preferences.Default.Beta = beta;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.LyricsHelper)
                .Subscribe(option => {
                    ActiveLyricsHelper.Inst.Set(option?.klass);
                    Preferences.Default.LyricHelper = option?.klass?.Name ?? string.Empty;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.LyricsHelperBrackets)
                .Subscribe(brackets => {
                    Preferences.Default.LyricsHelperBrackets = brackets;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.OtoEditor)
                .Subscribe(index => {
                    Preferences.Default.OtoEditor = index;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.NumRenderThreads)
                .Subscribe(index => {
                    Preferences.Default.NumRenderThreads = index;
                    HighThreads = index > SafeMaxThreadCount ? true : false;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.DefaultRenderer)
                .Subscribe(index => {
                    Preferences.Default.DefaultRenderer = index;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.OnnxRunner)
                .Subscribe(index => {
                    Preferences.Default.OnnxRunner = index;
                    Preferences.Save();
                    ToggleOnnxGpuDisplay(index == "DirectML");
                });
            this.WhenAnyValue(vm => vm.OnnxGpu)
                .Subscribe(index => {
                    Preferences.Default.OnnxGpu = index.deviceId;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.RememberMid)
                .Subscribe(index => {
                    Preferences.Default.RememberMid = index;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.RememberUst)
                .Subscribe(index => {
                    Preferences.Default.RememberUst = index;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.RememberVsqx)
                .Subscribe(index => {
                    Preferences.Default.RememberVsqx = index;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.ClearCacheOnQuit)
                .Subscribe(index => {
                    Preferences.Default.ClearCacheOnQuit = index;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.DiffSingerSteps)
                .Subscribe(index => {
                    Preferences.Default.DiffSingerSteps = index;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.DiffSingerStepsVariance)
                 .Subscribe(index => {
                     Preferences.Default.DiffSingerStepsVariance = index;
                     Preferences.Save();
                 });
            this.WhenAnyValue(vm => vm.DiffSingerStepsPitch)
                .Subscribe(index => {
                    Preferences.Default.DiffSingerStepsPitch = index;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.DiffSingerDepth)
                .Subscribe(index => {
                    Preferences.Default.DiffSingerDepth = index / 100;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.DiffSingerTensorCache)
                .Subscribe(useCache => {
                    Preferences.Default.DiffSingerTensorCache = useCache;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.DiffSingerLangCodeHide)
                .Subscribe(useCache => {
                    Preferences.Default.DiffSingerLangCodeHide = useCache;
                    Preferences.Save();
                });
            this.WhenAnyValue(vm => vm.SkipRenderingMutedTracks)
                .Subscribe(skipRenderingMutedTracks => {
                    Preferences.Default.SkipRenderingMutedTracks = skipRenderingMutedTracks;
                    Preferences.Save();
                });
        }

        public void TestAudioOutputDevice() {
            try {
                PlaybackManager.Inst.PlayTestSound();
            } catch (Exception e) {
                Log.Error(e, "Failed to play test sound.");
                DocManager.Inst.ExecuteCmd(new ErrorMessageNotification("Failed to play test sound.", e));
            }
        }

        public void OpenResamplerLocation() {
            try {
                string path = PathManager.Inst.ResamplersPath;
                Directory.CreateDirectory(path);
                OS.OpenFolder(path);
            } catch (Exception e) {
                DocManager.Inst.ExecuteCmd(new ErrorMessageNotification(e));
            }
        }

        public void SetAddlSingersPath(string path) {
            Preferences.Default.AdditionalSingerPath = path;
            Preferences.Save();
            this.RaisePropertyChanged(nameof(AdditionalSingersPath));
        }

        public void SetVLabelerPath(string path) {
            Preferences.Default.VLabelerPath = path;
            Preferences.Save();
            this.RaisePropertyChanged(nameof(VLabelerPath));
        }

        public void SetSetParamPath(string path) {
            Preferences.Default.SetParamPath = path;
            Preferences.Save();
            this.RaisePropertyChanged(nameof(SetParamPath));
        }

        public void SetWinePath(string path) {
            Preferences.Default.WinePath = path;
            Preferences.Save();
            ToolsManager.Inst.Initialize();
            this.RaisePropertyChanged(nameof(WinePath));
        }

        public void RefreshThemes() {
            this.RaisePropertyChanged(nameof(ThemeItems));
        }

        public void ToggleOnnxGpuDisplay(bool show) {
            ShowOnnxGpu = show;
        }
    }
}
