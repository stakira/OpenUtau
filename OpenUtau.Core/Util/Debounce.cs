using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenUtau.Core.Util {
    public class Debounce {
        CancellationTokenSource? cancellation = null;

        public void Do(TimeSpan timeSpan, Func<Task> callback) {
            cancellation?.Cancel();
            cancellation = new CancellationTokenSource();

            Task.Delay(timeSpan, cancellation.Token)
                .ContinueWith(async task => {
                    if (task.IsCompletedSuccessfully) {
                        await callback();
                    }
                });
        }
    }
}
