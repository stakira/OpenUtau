using System.Threading;
using System.Threading.Tasks;

namespace OpenUtau.Core.Render {
    /// <summary>
    /// Render result of a phrase.
    /// </summary>
    class RenderResult {
        public float[] samples;

        /// <summary>
        /// The length of leading samples.
        /// </summary>
        public double leadingMs;

        /// <summary>
        /// Start position of non-leading samples.
        /// </summary>
        public double positionMs;
    }

    /// <summary>
    /// Interface of phrase-based renderer.
    /// </summary>
    interface IRenderer {
        Task<RenderResult> Render(RenderPhrase phrase, Progress progress, CancellationTokenSource cancellation);
    }
}
