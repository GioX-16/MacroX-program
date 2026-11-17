using macrox.Mvvm;

namespace MacroX.Tests.Mvvm
{
    public class AsyncRelayCommandTests
    {
        private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(5);

        [Fact]
        public void Constructor_WithNullExecute_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new AsyncRelayCommand(null!));
        }

        [Fact]
        public void CanExecute_WithoutPredicate_ReturnsTrue()
        {
            var sut = new AsyncRelayCommand(() => Task.CompletedTask);

            Assert.True(sut.CanExecute(null));
        }

        [Fact]
        public void CanExecute_WithPredicate_ReturnsPredicateResult()
        {
            var allowed = false;
            var sut = new AsyncRelayCommand(() => Task.CompletedTask, () => allowed);

            Assert.False(sut.CanExecute(null));
            allowed = true;
            Assert.True(sut.CanExecute(null));
        }

        [Fact]
        public void Execute_WhenCanExecuteIsFalse_DoesNotInvokeDelegate()
        {
            var calls = 0;
            var sut = new AsyncRelayCommand(() =>
            {
                calls++;
                return Task.CompletedTask;
            }, () => false);

            sut.Execute(null);

            Assert.Equal(0, calls);
        }

        [Fact]
        public async Task Execute_WhileRunning_ReportsCannotExecuteAndIgnoresReentrantCalls()
        {
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var calls = 0;
            var sut = new AsyncRelayCommand(() =>
            {
                calls++;
                return gate.Task;
            });

            sut.Execute(null);

            Assert.False(sut.CanExecute(null));
            sut.Execute(null);
            Assert.Equal(1, calls);

            gate.SetResult();
            await WaitUntilAsync(() => sut.CanExecute(null));

            Assert.Equal(1, calls);
            Assert.True(sut.CanExecute(null));
        }

        [Fact]
        public async Task Execute_WhenDelegateCompletes_BlocksOnlyForItsDuration()
        {
            var calls = 0;
            var sut = new AsyncRelayCommand(async () =>
            {
                calls++;
                await Task.Yield();
            });

            sut.Execute(null);
            await WaitUntilAsync(() => sut.CanExecute(null));

            sut.Execute(null);
            await WaitUntilAsync(() => sut.CanExecute(null));

            Assert.Equal(2, calls);
        }

        private static async Task WaitUntilAsync(Func<bool> condition)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (!condition())
            {
                if (stopwatch.Elapsed > WaitTimeout)
                {
                    throw new TimeoutException("La condición esperada no se cumplió dentro del tiempo límite.");
                }

                await Task.Delay(10);
            }
        }
    }
}
