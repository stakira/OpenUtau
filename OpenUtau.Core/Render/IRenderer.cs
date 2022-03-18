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

        /// <summary>
        /// Length estimated before actual render.
        /// </summary>
        public double estimatedLengthMs;
    }

    /// <summary>
    /// Interface of phrase-based renderer.
    /// </summary>
    interface IRenderer {
        RenderResult Layout(RenderPhrase phrase);
        Task<RenderResult> Render(RenderPhrase phrase, Progress progress, CancellationTokenSource cancellation, bool isPreRender = false);
    }
}
