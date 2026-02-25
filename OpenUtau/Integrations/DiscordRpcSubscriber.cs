using System;
using System.Threading.Tasks;
using Serilog;
using OpenUtau.Core;

namespace OpenUtau
{
    class DiscordRpcSubscriber : ICmdSubscriber{
        public void OnNext(UCommand cmd, bool isUndo) {
            if (isUndo) return;
            if (cmd is LoadProjectNotification || cmd is SingersRefreshedNotification || cmd is OtoChangedNotification || cmd is TrackChangeSingerCommand) {
                Task.Run(async () => {
                    try {
                        Log.Information($"Updating Discord RPC on event {cmd.GetType().Name}");
                        DiscordRPC.RPC(DocManager.Inst.Project);
                    } catch (Exception e) {
                        Log.Warning(e, $"Discord RPC failed: {e.Message}");
                    }
                });
            }
        }
    }
}
