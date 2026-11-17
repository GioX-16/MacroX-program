using macrox.ViewModels;

namespace MacroX.Tests.ViewModels
{
    public class NavigationSectionTests
    {
        [Fact]
        public void Constructor_ExposesAllProvidedValues()
        {
            var sut = new NavigationSection("key", "Título", "Descripción", "\uE80F");

            Assert.Equal("key", sut.Key);
            Assert.Equal("Título", sut.Title);
            Assert.Equal("Descripción", sut.Description);
            Assert.Equal("\uE80F", sut.Icon);
        }

        [Fact]
        public void Equality_SameValues_AreEqual()
        {
            var left = new NavigationSection("home", "Inicio", "Desc", "A");
            var right = new NavigationSection("home", "Inicio", "Desc", "A");

            Assert.Equal(left, right);
            Assert.True(left == right);
            Assert.Equal(left.GetHashCode(), right.GetHashCode());
        }

        [Fact]
        public void Equality_DifferentValues_AreNotEqual()
        {
            var left = new NavigationSection("home", "Inicio", "Desc", "A");
            var right = new NavigationSection("home", "Inicio", "Desc", "B");

            Assert.NotEqual(left, right);
            Assert.False(left == right);
        }
    }
}
