using System.Threading;
using System.Threading.Tasks;
using OpenUtau.Core.SignalChain;

namespace OpenUtau.Core.Render {
    interface IRenderer {
        Task<ISignalSource> Render(RenderPhrase phrase, Progress progress, CancellationTokenSource cancellation);
    }
}
