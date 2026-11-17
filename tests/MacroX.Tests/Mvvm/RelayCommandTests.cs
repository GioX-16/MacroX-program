using macrox.Mvvm;

namespace MacroX.Tests.Mvvm
{
    public class RelayCommandTests
    {
        [Fact]
        public void Constructor_WithNullExecute_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new RelayCommand(null!));
        }

        [Fact]
        public void CanExecute_WithoutPredicate_ReturnsTrue()
        {
            var sut = new RelayCommand(() => { });

            Assert.True(sut.CanExecute(null));
            Assert.True(sut.CanExecute("cualquier parámetro"));
        }

        [Fact]
        public void CanExecute_WithPredicate_ReturnsPredicateResult()
        {
            var allowed = false;
            var sut = new RelayCommand(() => { }, () => allowed);

            Assert.False(sut.CanExecute(null));

            allowed = true;
            Assert.True(sut.CanExecute(null));
        }

        [Fact]
        public void Execute_InvokesActionWithIgnoredParameter()
        {
            var calls = 0;
            var sut = new RelayCommand(() => calls++);

            sut.Execute(null);
            sut.Execute("ignorado");

            Assert.Equal(2, calls);
        }

        [Fact]
        public void Execute_WhenCanExecuteIsFalse_StillInvokesAction()
        {
            var calls = 0;
            var sut = new RelayCommand(() => calls++, () => false);

            sut.Execute(null);

            Assert.Equal(1, calls);
        }

        [Fact]
        public void CanExecuteChanged_SubscribeAndUnsubscribe_DoesNotThrow()
        {
            var sut = new RelayCommand(() => { });
            EventHandler handler = (_, _) => { };

            var exception = Record.Exception(() =>
            {
                sut.CanExecuteChanged += handler;
                sut.CanExecuteChanged -= handler;
            });

            Assert.Null(exception);
        }

        [Fact]
        public void RaiseCanExecuteChanged_WithoutSubscribers_DoesNotThrow()
        {
            var exception = Record.Exception(RelayCommand.RaiseCanExecuteChanged);

            Assert.Null(exception);
        }
    }
}
