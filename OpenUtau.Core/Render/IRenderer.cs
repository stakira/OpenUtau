using System.Threading;
using System.Threading.Tasks;

namespace OpenUtau.Core.Render {
    interface IRenderer {
        Task<float[]> Render(RenderPhrase phrase, CancellationTokenSource cancellation);
    }
}
