using System;
using System.IO;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenUtau.Core.SignalChain;

namespace OpenUtau.Core.Network {
    public class VSTClient {
        private TcpClient client;
        private Stream stream;
        private VSTData data;
        public VSTClient(string host, int port = 1556) {
            this.port = port;
            this.host = host;
            Console.WriteLine("Created VST client");
        }

        ~VSTClient() {
            stream.Dispose();
            client.Dispose();
        }

        public static T ByteToType<T>(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));

            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();

            return theStructure;
        }

        public void Update() {
            if (client.Connected) {
                Console.WriteLine(data.playing);
                var project = DocManager.Inst.Project;
                data.ticks = data.ticks * project.BeatTicks / data.ticksPerBeat;
                DocManager.Inst.playPosTick = (int)data.ticks;
               /* if (PlaybackManager.Inst.Playing && PlaybackManager.Inst.AudioOutput.GetPosition() != (long)data.ticks) {
                    PlaybackManager.Inst.PlayOrPause();
                    PlaybackManager.Inst.PlayOrPause();
                }*/
                if (data.playing && !PlaybackManager.Inst.Playing) {
                    PlaybackManager.Inst.PlayOrPause();
                } else if (!data.playing && PlaybackManager.Inst.Playing) {
                    PlaybackManager.Inst.StopPlayback();
                }
            }
        }

        public void Connect() {
            client = new TcpClient();
            Console.WriteLine("Connecting to the tcp server");
            if (!client.ConnectAsync(host, port).Wait(5000)) {
                Console.WriteLine("An error occurred when trying to connect to the server");
                return;
            }
            Console.WriteLine("Connected to the server");
            stream = client.GetStream();
            while (client.Connected) {
                if (stream.CanRead) {
                    using BinaryReader reader = new BinaryReader(stream, Encoding.Default, true);
                    data = ByteToType<VSTData>(reader);
                    
                } else {
                    Console.WriteLine("Can't read stream");
                }
            }
        }

        private int port;
        private string host;
    }
}
