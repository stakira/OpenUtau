using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using OpenUtau.Core;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;
using Serilog;

namespace OpenUtau {
    enum DiscordRpcOp {
        Handshake = 0,
        Frame = 1,
        Close = 2,
        Ping = 3,
        Pong = 4
    }

    class DiscordRPC {
        public static volatile bool StopRpc = false;
        public static long StartTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        private static NamedPipeClientStream RpcPipe;
        private static Thread RpcPingThread;
        private static string ClientID { get; } = "1472598039125430416";
        private static BinaryWriter RpcWriter;
        private static BinaryReader RpcReader;

        public static void InitRPC() {
            if (!Preferences.Default.DiscordRichPresence) return;

            NamedPipeClientStream pipe = null;

            for (int i = 0; i < 10; i++) {
                try {
                    pipe = new NamedPipeClientStream(
                        ".",
                        $"discord-ipc-{i}",
                        PipeDirection.InOut,
                        PipeOptions.Asynchronous);

                    pipe.Connect(3000);
                    break;
                } catch (TimeoutException) { } catch (IOException) { }
            }

            if (pipe == null || !pipe.IsConnected) return;

            var reader = new BinaryReader(pipe, Encoding.UTF8, true);
            var writer = new BinaryWriter(pipe, Encoding.UTF8, true);

            string handshake = "{\"v\":1,\"client_id\":\"" + ClientID + "\"}";

            Write(writer, (int)DiscordRpcOp.Handshake, handshake);
            Read(reader);

            RpcPipe = pipe;
            RpcReader = reader;
            RpcWriter = writer;

            RpcPingThread = new Thread(() => {
                try {
                    while (pipe.IsConnected && !StopRpc) {
                        int opcode = reader.ReadInt32();
                        int length = reader.ReadInt32();
                        byte[] data = reader.ReadBytes(length);

                        if (opcode == (int)DiscordRpcOp.Ping) {
                            writer.Write((int)DiscordRpcOp.Pong);
                            writer.Write(length);
                            writer.Write(data);
                            writer.Flush();
                        } else if (opcode == (int)DiscordRpcOp.Close) {
                            break;
                        }
                    }
                } catch { } finally {
                    pipe.Dispose();
                    Log.Information("Discord RPC stopped");
                }

            });
            RpcPingThread.IsBackground = true;
            RpcPingThread.Start();
        }

        public static void RPC(UProject proj) {
            if (!Preferences.Default.DiscordRichPresence) return;

            NamedPipeClientStream pipe = RpcPipe;

            var reader = RpcReader;
            var writer = RpcWriter;

            List<string> singers = new List<string>();
            if (proj.tracks != null && proj.tracks.Count > 0) {
                foreach (UTrack track in proj.tracks) {
                    if (track.Singer != null) singers.Add(track.Singer.Name);
                }
            }
            
            if(singers.Count == 0) {
                singers.Add("no singers yet");
            }

            string activityJson = JsonSerializer.Serialize(new {
                cmd = "SET_ACTIVITY",
                nonce = Guid.NewGuid().ToString(),
                args = new {
                    pid = Environment.ProcessId,
                    activity = new {
                        state = $"Making a OpenUtau VSynth song with {string.Join(" and ", singers)}!",
                        timestamps = new { start = StartTime },
                        assets = new {
                            large_image = "large_icon",
                            large_text = "OpenUtau",
                        },
                        buttons = new[] {
                            new {
                                label = "Get OpenUtau",
                                url = "https://openutau.com"
                            }
                        }
                    }
                }
            });

            if (RpcWriter == null) return;
            Write(writer, (int)DiscordRpcOp.Frame, activityJson);   
        }

        public static void IdleRPC() {
            if (!Preferences.Default.DiscordRichPresence) return;

            NamedPipeClientStream pipe = RpcPipe;

            var reader = RpcReader;
            var writer = RpcWriter;

            string activityJson = JsonSerializer.Serialize(new {
                cmd = "SET_ACTIVITY",
                nonce = Guid.NewGuid().ToString(),
                args = new {
                    pid = Environment.ProcessId,
                    activity = new {
                        state = $"Loading",
                        timestamps = new { start = StartTime },
                        assets = new {
                            large_image = "large_icon",
                            large_text = "OpenUtau",
                        },
                        buttons = new[] {
                            new {
                                label = "Get OpenUtau",
                                url = "https://openutau.com"
                            }
                        }
                    }
                }
            });

            if (RpcWriter == null) return;
            Write(writer, (int)DiscordRpcOp.Frame, activityJson);
        }

        static void Write(BinaryWriter writer, int opcode, string json) {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            writer.Write(opcode);
            writer.Write(bytes.Length);
            writer.Write(bytes);
            writer.Flush();
        }

        static void Read(BinaryReader reader) {
            int opcode = reader.ReadInt32();
            int length = reader.ReadInt32();
            reader.ReadBytes(length);
        }
    }
}
