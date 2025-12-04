using KeyboardAutoSwitcher.UI;
using Shouldly;

namespace KeyboardAutoSwitcher.Tests.UI
{
    /// <summary>
    /// Unit tests for TrayApplicationContext helper classes and event args
    /// Note: The main TrayApplicationContext class requires Windows Forms runtime,
    /// so we focus on testing the supporting classes and event argument types.
    /// </summary>
    public class TrayApplicationContextTests
    {

        #region LayoutChangedEventArgs Tests


        [Fact]
        public void LayoutChangedEventArgs_Constructor_WithAllParameters_ShouldSetProperties()
        {
            // Arrange & Act
            LayoutChangedEventArgs args = new("Dvorak", true, true);

            // Assert
            args.LayoutName.ShouldBe("Dvorak");
            args.IsExternalKeyboard.ShouldBeTrue();
            args.IsInitial.ShouldBeTrue();
        }

        [Fact]
        public void LayoutChangedEventArgs_Constructor_WithDefaultIsInitial_ShouldBeFalse()
        {
            // Arrange & Act
            LayoutChangedEventArgs args = new("AZERTY", false);

            // Assert
            args.LayoutName.ShouldBe("AZERTY");
            args.IsExternalKeyboard.ShouldBeFalse();
            args.IsInitial.ShouldBeFalse();
        }

        [Fact]
        public void LayoutChangedEventArgs_WithDvorakLayout_ShouldStoreCorrectName()
        {
            // Arrange & Act
            LayoutChangedEventArgs args = new("English (United States) - Dvorak", true, false);

            // Assert
            args.LayoutName.ShouldContain("Dvorak");
        }

        [Fact]
        public void LayoutChangedEventArgs_WithFrenchLayout_ShouldStoreCorrectName()
        {
            // Arrange & Act
            LayoutChangedEventArgs args = new("French (France)", false, false);

            // Assert
            args.LayoutName.ShouldContain("French");
        }

        [Fact]
        public void LayoutChangedEventArgs_WithEmptyLayoutName_ShouldBeAllowed()
        {
            // Arrange & Act
            LayoutChangedEventArgs args = new("", false, false);

            // Assert
            args.LayoutName.ShouldBe("");
        }

        [Theory]
        [InlineData("Dvorak", true, true)]
        [InlineData("AZERTY", false, false)]
        [InlineData("QWERTY", true, false)]
        [InlineData("French (France)", false, true)]
        public void LayoutChangedEventArgs_VariousCombinations_ShouldWork(string layout, bool isExternal, bool isInitial)
        {
            // Arrange & Act
            LayoutChangedEventArgs args = new(layout, isExternal, isInitial);

            // Assert
            args.LayoutName.ShouldBe(layout);
            args.IsExternalKeyboard.ShouldBe(isExternal);
            args.IsInitial.ShouldBe(isInitial);
        }

        #endregion

        #region KeyboardStatusEventArgs Tests

        [Fact]
        public void KeyboardStatusEventArgs_WhenConnected_ShouldReturnTrue()
        {
            // Arrange & Act
            KeyboardStatusEventArgs args = new(true);

            // Assert
            args.IsConnected.ShouldBeTrue();
        }

        [Fact]
        public void KeyboardStatusEventArgs_WhenDisconnected_ShouldReturnFalse()
        {
            // Arrange & Act
            KeyboardStatusEventArgs args = new(false);

            // Assert
            args.IsConnected.ShouldBeFalse();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void KeyboardStatusEventArgs_Constructor_ShouldSetIsConnected(bool isConnected)
        {
            // Arrange & Act
            KeyboardStatusEventArgs args = new(isConnected);

            // Assert
            args.IsConnected.ShouldBe(isConnected);
        }

        #endregion

        #region Log Path Tests

        [Fact]
        public void LogPath_ShouldBeUnderCommonApplicationData()
        {
            // Arrange & Act
            string logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "KeyboardAutoSwitcher",
                "logs");

            // Assert
            logPath.ShouldContain("KeyboardAutoSwitcher");
            logPath.ShouldContain("logs");
        }

        [Fact]
        public void LogPath_ShouldBeAbsolutePath()
        {
            // Arrange & Act
            string logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "KeyboardAutoSwitcher",
                "logs");

            // Assert
            Path.IsPathRooted(logPath).ShouldBeTrue();
        }

        #endregion

    }
}

