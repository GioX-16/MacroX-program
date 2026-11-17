using macrox.Mvvm;

namespace MacroX.Tests.Mvvm
{
    public class ObservableObjectTests
    {
        private sealed class TestObservable : ObservableObject
        {
            private int _value;

            public int Value
            {
                get => _value;
                set => SetProperty(ref _value, value);
            }

            public void Raise(string? propertyName) => OnPropertyChanged(propertyName);
        }

        [Fact]
        public void SetProperty_WhenValueChanges_ReturnsTrueAndRaisesPropertyChanged()
        {
            var sut = new TestObservable();
            var raised = new List<string?>();
            sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

            var changed = sut.Value = 42;

            Assert.Equal(42, changed);
            Assert.Equal(new[] { nameof(TestObservable.Value) }, raised);
        }

        [Fact]
        public void SetProperty_WhenValueIsEqual_ReturnsFalseAndDoesNotRaisePropertyChanged()
        {
            var sut = new TestObservable { Value = 7 };
            var raised = 0;
            sut.PropertyChanged += (_, _) => raised++;

            sut.Value = 7;

            Assert.Equal(7, sut.Value);
            Assert.Equal(0, raised);
        }

        [Fact]
        public void OnPropertyChanged_WithExplicitName_RaisesThatName()
        {
            var sut = new TestObservable();
            string? received = null;
            sut.PropertyChanged += (_, e) => received = e.PropertyName;

            sut.Raise("Custom");

            Assert.Equal("Custom", received);
        }

        [Fact]
        public void OnPropertyChanged_WithoutSubscribers_DoesNotThrow()
        {
            var sut = new TestObservable();

            var exception = Record.Exception(() => sut.Raise(nameof(TestObservable.Value)));

            Assert.Null(exception);
        }
    }
}
