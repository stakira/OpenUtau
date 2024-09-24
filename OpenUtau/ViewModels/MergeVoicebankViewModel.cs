using Serilog;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ReactiveUI.Fody.Helpers;
using OpenUtau.Classic;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;

namespace OpenUtau.App.ViewModels {
    public class RenameItem{
        public string Name { get; set; }
        public string NewName { get; set; }
        public RenameItem(string name){
            Name = name;
            NewName = name;
        }
        public RenameItem(string name, string newName){
            Name = name;
            NewName = newName;
        }
    }
    public class MergeVoicebankViewModel : ViewModelBase {
        [Reactive] public int Step { get; set; }
        public List<ClassicSinger> Voicebanks{ get; set; }
        public ClassicSinger thisSinger;
        [Reactive] public ClassicSinger? OtherSinger { get; set; }
        [Reactive] public ObservableCollection<RenameItem> FolderRenames { get; set; }
        [Reactive] public ObservableCollection<RenameItem> SubbankRenames { get; set; }
        [Reactive] public ObservableCollection<RenameItem> VoiceColorRenames { get; set; }
        public MergeVoicebankViewModel(ClassicSinger thisVoicebank) {
            this.thisSinger = thisVoicebank;
            Step = 0;
            Voicebanks = SingerManager.Inst.SingerGroups[USingerType.Classic]
                .Where(s => s.Id != thisVoicebank.Id)
                .Cast<ClassicSinger>()
                .ToList();
            FolderRenames = new ObservableCollection<RenameItem>();
            SubbankRenames = new ObservableCollection<RenameItem>();
            VoiceColorRenames = new ObservableCollection<RenameItem>();
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
                    FolderRenames.Add(new RenameItem(".", autoSuffix(Path.GetFileName(OtherSinger.Location), existingDirs)));
                }
                foreach(string dir in dirsToAdd){
                    FolderRenames.Add(new RenameItem(dir, autoSuffix(dir, existingDirs)));
                }
            } else if(Step == 2){
                thisSinger.EnsureLoaded();
                OtherSinger.EnsureLoaded();
                SubbankRenames.Clear();
                List<string> existingSubbanks = thisSinger.Subbanks.Select(b => b.Suffix).ToList();
                List<string> subbanksToAdd = OtherSinger.Subbanks.Select(b => b.Suffix).ToList();
                foreach(string subbank in subbanksToAdd){
                    SubbankRenames.Add(new RenameItem(subbank, autoSuffix(subbank, existingSubbanks)));
                }
            } else if(Step == 3){
                VoiceColorRenames.Clear();
                List<string> existingVoiceColors = thisSinger.Subbanks.Select(b => b.Color).Distinct().ToList();
                List<string> voiceColorsToAdd = OtherSinger.Subbanks.Select(b => b.Color).Distinct().ToList();
                foreach(string voiceColor in voiceColorsToAdd){
                    VoiceColorRenames.Add(new RenameItem(voiceColor, autoSuffix(voiceColor, existingVoiceColors)));
                }
            }
        }

        void ConvertOto(string fromPath, string toPath){
            if(OtherSinger == null){
                return;
            }
            //convert aliases
            
            var otoSet = VoicebankLoader.ParseOtoSet(fromPath, OtherSinger.TextFileEncoding, OtherSinger.UseFilenameAsAlias);
            using (var stream = File.Open(toPath, FileMode.Create, FileAccess.Write)){
                VoicebankLoader.WriteOtoSet(otoSet, stream, thisSinger.TextFileEncoding);
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
                    foreach(RenameItem folder in FolderRenames){
                        //Create folders
                        Directory.CreateDirectory(Path.Join(thisSinger.Location, folder.NewName));
                        //convert oto.ini
                        if(folder.Name == "."){
                            //File.Copy(Path.Join(OtherSinger.Location, "oto.ini"), Path.Join(thisSinger.Location, folder.NewName, "oto.ini"));
                        } else {
                            /*Directory.CreateDirectory(Path.Join(thisSinger.Location, folder.NewName));
                            foreach(string file in Directory.GetFiles(Path.Join(OtherSinger.Location, folder.Name))){
                                File.Copy(file, Path.Join(thisSinger.Location, folder.NewName, Path.GetFileName(file)));
                            }
                            foreach(string dir in Directory.GetDirectories(Path.Join(OtherSinger.Location, folder.Name))){
                                Directory.CreateDirectory(Path.Join(thisSinger.Location, folder.NewName, Path.GetFileName(dir)));
                                foreach(string file in Directory.GetFiles(dir)){
                                    File.Copy(file, Path.Join(thisSinger.Location, folder.NewName, Path.GetFileName(dir), Path.GetFileName(file)));
                                }
                            }*/
                        }
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