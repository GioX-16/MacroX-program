using macrox.ViewModels;

namespace MacroX.Tests.ViewModels
{
    public class MainViewModelTests
    {
        [Fact]
        public void Constructor_ExposesFourNavigationSections()
        {
            var sut = new MainViewModel();

            Assert.Equal(4, sut.Sections.Count);
        }

        [Fact]
        public void Constructor_SectionKeys_AreUniqueAndNonEmpty()
        {
            var sut = new MainViewModel();

            var keys = sut.Sections.Select(s => s.Key).ToList();

            Assert.All(keys, key => Assert.False(string.IsNullOrWhiteSpace(key)));
            Assert.Equal(keys.Count, keys.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.Contains("home", keys);
            Assert.Contains("macros", keys);
            Assert.Contains("excel", keys);
            Assert.Contains("settings", keys);
        }

        [Fact]
        public void Constructor_EverySectionHasTitleDescriptionAndIcon()
        {
            var sut = new MainViewModel();

            Assert.All(sut.Sections, section =>
            {
                Assert.False(string.IsNullOrWhiteSpace(section.Title));
                Assert.False(string.IsNullOrWhiteSpace(section.Description));
                Assert.False(string.IsNullOrWhiteSpace(section.Icon));
            });
        }

        [Fact]
        public void Constructor_SelectsFirstSection()
        {
            var sut = new MainViewModel();

            Assert.Same(sut.Sections[0], sut.SelectedSection);
            Assert.Equal("home", sut.SelectedSection.Key);
        }

        [Fact]
        public void Sections_CannotBeMutatedThroughPublicApi()
        {
            var sut = new MainViewModel();

            Assert.IsAssignableFrom<IReadOnlyList<NavigationSection>>(sut.Sections);
            Assert.False(sut.Sections is List<NavigationSection>);
        }

        [Fact]
        public void SelectedSection_WhenChanged_UpdatesValueAndRaisesPropertyChanged()
        {
            var sut = new MainViewModel();
            var raised = new List<string?>();
            sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

            var target = sut.Sections[2];
            sut.SelectedSection = target;

            Assert.Same(target, sut.SelectedSection);
            Assert.Equal(
                new[] { nameof(MainViewModel.SelectedSection), nameof(MainViewModel.CurrentPage) },
                raised);
        }

        [Fact]
        public void SelectedSection_WhenSetToSameInstance_DoesNotRaisePropertyChanged()
        {
            var sut = new MainViewModel();
            var raised = 0;
            sut.PropertyChanged += (_, _) => raised++;

            sut.SelectedSection = sut.Sections[0];

            Assert.Equal(0, raised);
        }

        [Fact]
        public void IsBusy_WhenToggled_RaisesPropertyChangedAndKeepsValue()
        {
            var sut = new MainViewModel();
            var raised = new List<string?>();
            sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

            Assert.False(sut.IsBusy);

            sut.IsBusy = true;

            Assert.True(sut.IsBusy);
            Assert.Equal(new[] { nameof(MainViewModel.IsBusy) }, raised);
        }

        [Fact]
        public void StatusMessage_WhenChanged_RaisesPropertyChanged()
        {
            var sut = new MainViewModel();
            var raised = new List<string?>();
            sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

            Assert.Equal("Listo", sut.StatusMessage);

            sut.StatusMessage = "Trabajando";

            Assert.Equal("Trabajando", sut.StatusMessage);
            Assert.Equal(new[] { nameof(MainViewModel.StatusMessage) }, raised);
        }

        [Fact]
        public void BusyMessage_HasNonEmptyDefaultAndNotifiesChanges()
        {
            var sut = new MainViewModel();
            var raised = new List<string?>();
            sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

            Assert.False(string.IsNullOrWhiteSpace(sut.BusyMessage));

            sut.BusyMessage = "Importando libro…";

            Assert.Equal("Importando libro…", sut.BusyMessage);
            Assert.Equal(new[] { nameof(MainViewModel.BusyMessage) }, raised);
        }

        [Fact]
        public void ApplicationTitleAndSubtitle_AreNonEmpty()
        {
            var sut = new MainViewModel();

            Assert.Equal("MacroX", sut.ApplicationTitle);
            Assert.False(string.IsNullOrWhiteSpace(sut.ApplicationSubtitle));
        }

        [Fact]
        public void DisplayVersion_MatchesAssemblyVersion()
        {
            var sut = new MainViewModel();

            var assemblyVersion = Assert.IsType<Version>(typeof(MainViewModel).Assembly.GetName().Version);
            Assert.Equal($"v{assemblyVersion.ToString(3)}", sut.DisplayVersion);
            Assert.Matches(@"^v\d+\.\d+\.\d+$", sut.DisplayVersion);
        }

        [Fact]
        public void DisplayVersion_DoesNotThrowWhenAssemblyVersionIsUnavailable()
        {
            // Guarda de regresión: el formato no debe depender de recursos de la UI.
            var exception = Record.Exception(() => _ = new MainViewModel().DisplayVersion);

            Assert.Null(exception);
        }
    }
}
