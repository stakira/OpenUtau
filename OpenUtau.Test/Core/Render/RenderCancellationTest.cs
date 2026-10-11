using System;
using System.Threading;
using System.Threading.Tasks;
using OpenUtau.Core.Render;
using Xunit;

namespace OpenUtau.Test.Core.Render {
    public class RenderCancellationTest {
        [Fact]
        public void CompletedRendererReturnsItsAudio() {
            using var cancellation = new CancellationTokenSource();
            var result = new RenderResult { samples = new float[] { .25f } };
            Assert.Same(result, RenderEngine.WaitForRenderer(Task.FromResult(result), cancellation));
        }

        [Fact]
        public void SupersededRendererCancellationIsExpected() {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            Assert.Null(RenderEngine.WaitForRenderer(Task.FromCanceled<RenderResult>(cancellation.Token), cancellation));
        }

        [Fact]
        public void WrappedCancellationIsAlsoExpectedWhenRequested() {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var task = Task.FromException<RenderResult>(new AggregateException(new AggregateException(new TaskCanceledException())));
            Assert.Null(RenderEngine.WaitForRenderer(task, cancellation));
        }

        [Fact]
        public void RealFailureIsNotSuppressedByCancellation() {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var failure = new InvalidOperationException("model failed");
            Assert.Same(failure, Assert.Throws<InvalidOperationException>(() =>
                RenderEngine.WaitForRenderer(Task.FromException<RenderResult>(failure), cancellation)));
            var mixed = Task.FromException<RenderResult>(new AggregateException(new TaskCanceledException(), failure));
            Assert.Throws<AggregateException>(() => RenderEngine.WaitForRenderer(mixed, cancellation));
        }

        [Fact]
        public void UnrequestedCancellationIsNotSuppressed() {
            using var cancellation = new CancellationTokenSource();
            using var other = new CancellationTokenSource();
            other.Cancel();
            Assert.ThrowsAny<OperationCanceledException>(() =>
                RenderEngine.WaitForRenderer(Task.FromCanceled<RenderResult>(other.Token), cancellation));
        }
    }
}
