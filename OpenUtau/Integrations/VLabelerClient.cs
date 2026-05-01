using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using K4os.Hash.xxHash;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using OpenUtau.Core;
using Serilog;

/*
 * Made And Checked By DELTA SYNTH & Gemini AI
 * Original Author: OpenUtau Team & Delta
 */

namespace OpenUtau.Integrations {
    /// <summary>
    /// ระบบไคลเอนต์สำหรับเชื่อมต่อและสั่งการ vLabeler ผ่านโปรโตคอล Network
    /// </summary>
    internal class VLabelerClient : Core.Util.SingletonBase<VLabelerClient> {
        
        // --- โครงสร้างข้อมูลสำหรับการสื่อสาร JSON ---

        class HeartbeatRequest {
            public string type = "Heartbeat";
            public long sentAt = Epoch();
        }

        class GotoEntryByName {
            public string parentFolderName;
            public string entryName;
            public GotoEntryByName(string parentFolderName, string entryName) {
                this.parentFolderName = parentFolderName;
                this.entryName = entryName;
            }
        }

        class TypedValue {
            public string type;
            public object value;
            public TypedValue(string type, object value) {
                this.type = type;
                this.value = value;
            }
        }

        class NewProjectArgs {
            public string labelerName = "utau-singer.default";
            public string? sampleDirectory;
            public string? cacheDirectory;
            public Dictionary<string, TypedValue>? labelerParams;
            public string? pluginName;
            public Dictionary<string, TypedValue>? pluginParams;
            public string? inputFile;
            public string encoding = Encoding.UTF8.WebName;
            public bool autoExport;
        }

        class OpenOrCreateRequest {
            public string type = "OpenOrCreate";
            public string projectFile = string.Empty;
            public GotoEntryByName? gotoEntryByName;
            public NewProjectArgs newProjectArgs = new NewProjectArgs();
            public long sentAt = Epoch();
        }

        // --- ระบบคำนวณและตัวช่วยจัดการข้อมูล ---

        private static long Epoch() {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }

        private static string HashHex(string s) {
            return $"{XXH32.DigestOf(Encoding.UTF8.GetBytes(s)):x8}";
        }

        /// <summary>
        /// ตรวจสอบการเชื่อมต่อกับ vLabeler (ส่งสัญญาณชีพ)
        /// </summary>
        private bool Heartbeat() {
            try {
                using (var client = new RequestSocket()) {
                    client.Connect("tcp://localhost:32342");
                    string reqStr = JsonConvert.SerializeObject(new HeartbeatRequest());
                    client.SendFrame(reqStr);
                    return client.TryReceiveFrameString(TimeSpan.FromMilliseconds(500), out _);
                }
            } catch {
                return false;
            }
        }

        /// <summary>
        /// สั่งให้ vLabeler เปิดโปรเจกต์เดิมหรือสร้างใหม่เพื่อแก้ไขเสียงที่เลือก
        /// </summary>
        private void OpenOrCreate(Core.Ustx.USinger singer, Core.Ustx.UOto? oto) {
            var existingProjectName = Directory.GetFiles(singer.Location)
                .Where(path => Path.GetExtension(path).ToLower() == ".lbp")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();

            var request = new OpenOrCreateRequest() {
                projectFile = Path.Combine(singer.Location, existingProjectName != null ? Path.GetFileName(existingProjectName) : "_vlabeler.lbp"),
                newProjectArgs = new NewProjectArgs {
                    cacheDirectory = Path.Combine(PathManager.Inst.CachePath, $"vlabeler-{HashHex(singer.Id)}"),
                    labelerParams = new Dictionary<string, TypedValue> {
                        { "useRootDirectory", new TypedValue("boolean", true) }
                    },
                    encoding = singer.TextFileEncoding.WebName,
                    autoExport = true,
                },
            };

            if (oto != null) {
                // ปรับปรุง Path ให้รองรับมาตรฐาน vLabeler
                request.gotoEntryByName = new GotoEntryByName(oto.Set.Replace("\\", "/"), oto.Alias);
            }

            using (var client = new RequestSocket()) {
                client.Connect("tcp://localhost:32342");
                string reqStr = JsonConvert.SerializeObject(request);
                client.SendFrame(reqStr);
                if (!client.TryReceiveFrameString(TimeSpan.FromMilliseconds(2000), out _)) {
                    Log.Warning($"[vLabeler] ไม่สามารถตอบสนองคำสั่ง OpenOrCreate ได้");
                }
            }
        }

        /// <summary>
        /// พยายามเริ่มต้นโปรแกรม vLabeler หากยังไม่ได้เปิดใช้งาน
        /// </summary>
        private Task<bool> TryStart() {
            return Task.Run(() => {
                if (Heartbeat()) return true;

                var path = Core.Util.Preferences.Default.VLabelerPath;
                if (!OS.AppExists(path)) {
                    throw new FileNotFoundException($"ไม่พบไฟล์โปรแกรม vLabeler ที่ตำแหน่ง: {path}");
                }

                using (var proc = new Process()) {
                    if (OS.IsMacOS()) {
                        OS.OpenFolder(path);
                    } else {
                        proc.StartInfo = new ProcessStartInfo(path) {
                            UseShellExecute = true,
                            CreateNoWindow = false,
                        };
                        proc.Start();
                    }
                    Log.Information("กำลังเริ่มต้นการทำงานของ vLabeler...");
                }

                // รอการเชื่อมต่อสูงสุด 5 วินาที
                for (int i = 0; i < 50; i++) {
                    Task.Delay(100).Wait();
                    if (Heartbeat()) {
                        Log.Information("vLabeler พร้อมใช้งานแล้ว");
                        return true;
                    }
                }
                return false;
            });
        }

        /// <summary>
        /// คำสั่งหลักในการส่งข้อมูลนักร้องและตัวโน้ตไปยัง vLabeler
        /// </summary>
        public void GotoOto(Core.Ustx.USinger singer, Core.Ustx.UOto? oto) {
            TryStart().ContinueWith(task => {
                if (!task.IsFaulted && task.Result) {
                    OpenOrCreate(singer, oto);
                } else {
                    string errorMsg = task.IsFaulted ? task.Exception?.InnerException?.Message ?? "เกิดข้อผิดพลาดในการเปิดโปรแกรม" : "ไม่สามารถเริ่มต้น vLabeler ได้";
                    DocManager.Inst.ExecuteCmd(new ErrorMessageNotification($"ข้อผิดพลาดของ vLabeler: {errorMsg}"));
                }
            }, TaskScheduler.Default);
        }
    }
}
