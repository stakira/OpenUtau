using Serilog;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ReactiveUI.Fody.Helpers;
using OpenUtau.Classic;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using SharpCompress;
using DynamicData;

namespace OpenUtau.App.ViewModels {
    public class ConvertItem{
        public string Name { get; set; }
        public string NewName { get; set; }
        public ConvertItem(string name){
            Name = name;
            NewName = name;
        }
        public ConvertItem(string name, string newName){
            Name = name;
            NewName = newName;
        }
        public override string ToString(){
            return $"{Name} -> {NewName}";
        }
    }
    public class MergeVoicebankViewModel : ViewModelBase {
        [Reactive] public int Step { get; set; }
        public List<ClassicSinger> Voicebanks{ get; set; }
        public ClassicSinger thisSinger;
        [Reactive] public ClassicSinger? OtherSinger { get; set; }
        [Reactive] public ObservableCollection<ConvertItem> FolderRenames { get; set; }
        [Reactive] public ObservableCollection<ConvertItem> SubbankRenames { get; set; }
        [Reactive] public ObservableCollection<ConvertItem> VoiceColorRenames { get; set; }
        string[] supportedAudioTypes = new string[]{".wav", ".flac", ".ogg", ".mp3", ".aiff", ".aif", ".aifc"};
        public MergeVoicebankViewModel(ClassicSinger thisVoicebank) {
            this.thisSinger = thisVoicebank;
            Step = 0;
            Voicebanks = SingerManager.Inst.SingerGroups[USingerType.Classic]
                .Where(s => s.Id != thisVoicebank.Id)
                .Cast<ClassicSinger>()
                .ToList();
            FolderRenames = new ObservableCollection<ConvertItem>();
            SubbankRenames = new ObservableCollection<ConvertItem>();
            VoiceColorRenames = new ObservableCollection<ConvertItem>();
        }

        /// <summary>
        /// Automatically suffixes a name with a number if it already exists in a list of names.
        /// </summary>
        /// <param name="name">The name to suffix.</param>
        /// <param name="existingNames">The list of names to check against.</param>
        /// <returns>The suffixed name.</returns>
        string autoSuffix(string name, IList<string> existingNames){
            if(!existingNames.Contains(name)){
                return name;
            }
            int i = 1;
            while(existingNames.Contains(name + "_" + i)){
                i++;
            }
            return name + "_" + i;
        }

        public void Next(){
            if(OtherSinger == null){
                return;
            }
            Step++;
            if(Step == 1){
                FolderRenames.Clear();
                string l = thisSinger.Location;
                //For this voicebank, get all the directories and files in the voicebank's location (that file copied here can't use)
                List<string> existingDirs = Directory.GetFiles(thisSinger.Location)
                    .Concat(Directory.GetDirectories(thisSinger.Location))
                    .Select(d => Path.GetFileName(d))
                    .ToList();
                //For the other voicebank, get all the subfolders that contain an oto.ini file directly or indirectly.
                List<string> dirsToAdd = Directory.GetDirectories(OtherSinger.Location)
                    .Where(d => Directory.EnumerateFiles(d, "oto.ini", SearchOption.AllDirectories).Any())
                    .Select(d => Path.GetFileName(d))
                    .ToList();
                if(File.Exists(Path.Join(OtherSinger.Location, "oto.ini"))){
                    FolderRenames.Add(new ConvertItem(".", autoSuffix(Path.GetFileName(OtherSinger.Location), existingDirs)));
                }
                foreach(string dir in dirsToAdd){
                    FolderRenames.Add(new ConvertItem(dir, autoSuffix(dir, existingDirs)));
                }
            } else if(Step == 2){
                thisSinger.EnsureLoaded();
                OtherSinger.EnsureLoaded();
                SubbankRenames.Clear();
                List<string> existingSubbanks = thisSinger.Subbanks.Select(b => $"{b.Prefix},{b.Suffix}").ToList();
                List<string> subbanksToAdd = OtherSinger.Subbanks.Select(b => $"{b.Prefix},{b.Suffix}").ToList();
                foreach(string subbank in subbanksToAdd){
                    SubbankRenames.Add(new ConvertItem(subbank, autoSuffix(subbank, existingSubbanks)));
                }
            } else if(Step == 3){
                VoiceColorRenames.Clear();
                List<string> existingVoiceColors = thisSinger.Subbanks.Select(b => b.Color).Distinct().ToList();
                List<string> voiceColorsToAdd = OtherSinger.Subbanks.Select(b => b.Color).Distinct().ToList();
                foreach(string voiceColor in voiceColorsToAdd){
                    VoiceColorRenames.Add(new ConvertItem(voiceColor, autoSuffix(voiceColor, existingVoiceColors)));
                }
            }
        }

        void ConvertOto(string fromPath, string toPath, List<Subbank> oldSubbanks, List<Subbank> newSubbanks){
            if(OtherSinger == null){
                return;
            }
            if(!File.Exists(fromPath)){
                Log.Error($"File {fromPath} does not exist");
                return;
            }
            //convert aliases
            var patterns = oldSubbanks.Select(subbank => new Regex($"^{Regex.Escape(subbank.Prefix)}(.*){Regex.Escape(subbank.Suffix)}$"))
                .ToList();
            var otoSet = VoicebankLoader.ParseOtoSet(fromPath, OtherSinger.TextFileEncoding, OtherSinger.UseFilenameAsAlias);
            foreach (var oto in otoSet.Otos){
                if (!oto.IsValid) {
                    if (!string.IsNullOrEmpty(oto.Error)) {
                        Log.Error(oto.Error);
                    }
                    continue;
                }
                for (var i = 0; i < patterns.Count; i++) {
                    var m = patterns[i].Match(oto.Alias);
                    if (m.Success) {
                        oto.Alias = newSubbanks[i].Prefix + m.Groups[1].Value + newSubbanks[i].Suffix;
                        break;
                    }
                }
            }
            using (var stream = File.Open(toPath, FileMode.Create, FileAccess.Write)){
                VoicebankLoader.WriteOtoSet(otoSet, stream, thisSinger.TextFileEncoding);
            }
        }

        Subbank ConvertSubBank(Subbank oldSubbank){
            var newName = SubbankRenames.First(r=>r.Name == $"{oldSubbank.Prefix},{oldSubbank.Suffix}").NewName;
            var newColor = VoiceColorRenames.First(r=>r.Name == oldSubbank.Color).NewName;
            if(newName.Contains(",")){
                var n = newName.Split(",");
                return new Subbank(){
                    Prefix = n[0],
                    Suffix = n[^1],
                    Color = newColor,
                    ToneRanges = oldSubbank.ToneRanges
                };
            } else {
                return new Subbank(){
                    Prefix = "",
                    Suffix = newName,
                    Color = newColor,
                    ToneRanges = oldSubbank.ToneRanges 
                };
            }
        }

        public Task Merge(){
            return Task.Run(() => {
                try {
                    Log.Information($"Merging voicebank {OtherSinger} to {thisSinger}");
                    if(OtherSinger == null){
                        Log.Error("Other singer is null");
                        return;
                    }
                    //convert subbanks
                    var oldSubbanks = OtherSinger.Subbanks
                        .OrderByDescending(subbank => subbank.Prefix.Length + subbank.Suffix.Length)
                        .Select(b => b.subbank)
                        .ToList();
                    var newSubbanks = oldSubbanks
                        .Select(ConvertSubBank)
                        .ToList();
                    var otosToConvert = new List<ConvertItem>();
                    var filesToCopy = new List<ConvertItem>();
                    foreach(ConvertItem folder in FolderRenames){
                        //Create folders
                        Directory.CreateDirectory(Path.Join(thisSinger.Location, folder.NewName));
                        //Add oto.ini and audio files in one folder to list of files to copy, not recursive
                        void AddFolder(string fromDir, string toDir){
                            if(File.Exists(Path.Join(fromDir, "oto.ini"))){
                                Directory.CreateDirectory(toDir);
                                otosToConvert.Add(new ConvertItem(
                                    Path.Join(fromDir, "oto.ini"), 
                                    Path.Join(toDir, "oto.ini")
                                ));
                                filesToCopy.AddRange(
                                    Directory.GetFiles(fromDir)
                                        .Where(f => supportedAudioTypes.Contains(Path.GetExtension(f)))
                                        .Select(f => new ConvertItem(f, Path.Join(toDir, Path.GetFileName(f))))
                                    );
                                
                            }
                        }
                        if(folder.Name == "."){
                            AddFolder(OtherSinger.Location, Path.Join(thisSinger.Location, folder.NewName));
                        } else {
                            string currentFolder = Path.Join(OtherSinger.Location, folder.Name);
                            Directory.EnumerateFiles(currentFolder, "oto.ini", SearchOption.AllDirectories)
                                .Select(d => Path.GetDirectoryName(d)!)
                                .ForEach(d => AddFolder(d, Path.Join(thisSinger.Location, folder.NewName, Path.GetRelativePath(currentFolder, d))));
                        }
                    }
                    var totalFiles = otosToConvert.Count + filesToCopy.Count;
                    var progress = 0;
                    //Convert oto.ini
                    foreach(ConvertItem oto in otosToConvert){
                        DocManager.Inst.ExecuteCmd(new ProgressBarNotification(progress++ * 100.0 / totalFiles, $"{oto.NewName} <= {oto.Name}"));
                        ConvertOto(oto.Name, oto.NewName, oldSubbanks, newSubbanks);
                    }
                    //Copy audio files
                    foreach(ConvertItem file in filesToCopy){
                        DocManager.Inst.ExecuteCmd(new ProgressBarNotification(progress++ * 100.0 / totalFiles, $"{file.NewName} <= {file.Name}"));
                        File.Copy(file.Name, file.NewName);
                    }
                    //Edit voice color of this singer
                    var yamlFile = Path.Combine(thisSinger.Location, "character.yaml");
                    VoicebankConfig? bankConfig = null;
                    try {
                        // Load from character.yaml
                        if (File.Exists(yamlFile)) {
                            using (var stream = File.OpenRead(yamlFile)) {
                                bankConfig = VoicebankConfig.Load(stream);
                            }
                        }
                    } catch { }
                    if (bankConfig == null) {
                        bankConfig = new VoicebankConfig();
                    }
                    bankConfig.Subbanks = bankConfig.Subbanks.Concat(newSubbanks).ToArray();
                    using (var stream = File.Open(yamlFile, FileMode.Create)) {
                    bankConfig.Save(stream);
                }

                } finally {
                    new Task(() => {
                        DocManager.Inst.ExecuteCmd(new ProgressBarNotification(0, ""));
                        DocManager.Inst.ExecuteCmd(new SingersChangedNotification());
                    }).Start(DocManager.Inst.MainScheduler);
                }
            });
        }
    }
}