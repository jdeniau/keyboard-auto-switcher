using KeyboardAutoSwitcher.UI;
using Shouldly;
using Xunit;

namespace KeyboardAutoSwitcher.Tests;

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
        var args = new LayoutChangedEventArgs("Dvorak", true, true);

        // Assert
        args.LayoutName.ShouldBe("Dvorak");
        args.IsExternalKeyboard.ShouldBeTrue();
        args.IsInitial.ShouldBeTrue();
    }

    [Fact]
    public void LayoutChangedEventArgs_Constructor_WithDefaultIsInitial_ShouldBeFalse()
    {
        // Arrange & Act
        var args = new LayoutChangedEventArgs("AZERTY", false);

        // Assert
        args.LayoutName.ShouldBe("AZERTY");
        args.IsExternalKeyboard.ShouldBeFalse();
        args.IsInitial.ShouldBeFalse();
    }

    [Fact]
    public void LayoutChangedEventArgs_WithDvorakLayout_ShouldStoreCorrectName()
    {
        // Arrange & Act
        var args = new LayoutChangedEventArgs("English (United States) - Dvorak", true, false);

        // Assert
        args.LayoutName.ShouldContain("Dvorak");
    }

    [Fact]
    public void LayoutChangedEventArgs_WithFrenchLayout_ShouldStoreCorrectName()
    {
        // Arrange & Act
        var args = new LayoutChangedEventArgs("French (France)", false, false);

        // Assert
        args.LayoutName.ShouldContain("French");
    }

    [Fact]
    public void LayoutChangedEventArgs_WithEmptyLayoutName_ShouldBeAllowed()
    {
        // Arrange & Act
        var args = new LayoutChangedEventArgs("", false, false);

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
        var args = new LayoutChangedEventArgs(layout, isExternal, isInitial);

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
        var args = new KeyboardStatusEventArgs(true);

        // Assert
        args.IsConnected.ShouldBeTrue();
    }

    [Fact]
    public void KeyboardStatusEventArgs_WhenDisconnected_ShouldReturnFalse()
    {
        // Arrange & Act
        var args = new KeyboardStatusEventArgs(false);

        // Assert
        args.IsConnected.ShouldBeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void KeyboardStatusEventArgs_Constructor_ShouldSetIsConnected(bool isConnected)
    {
        // Arrange & Act
        var args = new KeyboardStatusEventArgs(isConnected);

        // Assert
        args.IsConnected.ShouldBe(isConnected);
    }

    #endregion

    #region Layout Name Detection Tests (simulating UpdateIcon logic)

    [Theory]
    [InlineData("English (United States) - Dvorak", true)]
    [InlineData("Dvorak", true)]
    [InlineData("dvorak", true)]
    [InlineData("DVORAK", true)]
    [InlineData("US Dvorak", true)]
    public void LayoutName_ContainsDvorak_ShouldBeDetected(string layoutName, bool expected)
    {
        // This tests the logic used in UpdateIcon method
        // Arrange & Act
        var containsDvorak = layoutName.Contains("Dvorak", StringComparison.OrdinalIgnoreCase);

        // Assert
        containsDvorak.ShouldBe(expected);
    }

    [Theory]
    [InlineData("French (France)", true)]
    [InlineData("french", true)]
    [InlineData("AZERTY", true)]
    [InlineData("azerty", true)]
    [InlineData("German", false)]
    public void LayoutName_ContainsFrenchOrAzerty_ShouldBeDetected(string layoutName, bool expected)
    {
        // This tests the logic used in UpdateIcon method
        // Arrange & Act
        var containsFrenchOrAzerty = layoutName.Contains("French", StringComparison.OrdinalIgnoreCase) ||
                                      layoutName.Contains("AZERTY", StringComparison.OrdinalIgnoreCase);

        // Assert
        containsFrenchOrAzerty.ShouldBe(expected);
    }

    [Fact]
    public void LayoutName_IconSelection_DvorakHasPriorityOverFrench()
    {
        // If a layout name somehow contains both Dvorak and French,
        // Dvorak should be selected first (based on the if-else order in UpdateIcon)
        // Arrange
        var layoutName = "French Dvorak Custom";

        // Act - Simulate UpdateIcon logic
        string selectedIcon;
        if (layoutName.Contains("Dvorak", StringComparison.OrdinalIgnoreCase))
        {
            selectedIcon = "Dvorak";
        }
        else if (layoutName.Contains("French", StringComparison.OrdinalIgnoreCase) ||
                 layoutName.Contains("AZERTY", StringComparison.OrdinalIgnoreCase))
        {
            selectedIcon = "AZERTY";
        }
        else
        {
            selectedIcon = "Default";
        }

        // Assert
        selectedIcon.ShouldBe("Dvorak");
    }

    [Fact]
    public void LayoutName_UnknownLayout_ShouldUseDefault()
    {
        // Arrange
        var layoutName = "German (Germany)";

        // Act - Simulate UpdateIcon logic
        string selectedIcon;
        if (layoutName.Contains("Dvorak", StringComparison.OrdinalIgnoreCase))
        {
            selectedIcon = "Dvorak";
        }
        else if (layoutName.Contains("French", StringComparison.OrdinalIgnoreCase) ||
                 layoutName.Contains("AZERTY", StringComparison.OrdinalIgnoreCase))
        {
            selectedIcon = "AZERTY";
        }
        else
        {
            selectedIcon = "Default";
        }

        // Assert
        selectedIcon.ShouldBe("Default");
    }

    #endregion

    #region Keyboard Status Text Tests (simulating OnKeyboardStatusChanged logic)

    [Fact]
    public void KeyboardStatusText_WhenConnected_ShouldShowConnectedMessage()
    {
        // Arrange
        var isConnected = true;

        // Act - Simulate OnKeyboardStatusChanged logic
        var text = isConnected
            ? "Clavier: TypeMatrix connecté ✓"
            : "Clavier: TypeMatrix non détecté";

        // Assert
        text.ShouldBe("Clavier: TypeMatrix connecté ✓");
        text.ShouldContain("✓");
    }

    [Fact]
    public void KeyboardStatusText_WhenDisconnected_ShouldShowDisconnectedMessage()
    {
        // Arrange
        var isConnected = false;

        // Act - Simulate OnKeyboardStatusChanged logic
        var text = isConnected
            ? "Clavier: TypeMatrix connecté ✓"
            : "Clavier: TypeMatrix non détecté";

        // Assert
        text.ShouldBe("Clavier: TypeMatrix non détecté");
        text.ShouldNotContain("✓");
    }

    #endregion

    #region Balloon Notification Logic Tests

    [Fact]
    public void BalloonNotification_ShouldNotShowOnInitialLayout()
    {
        // Arrange
        var layoutArgs = new LayoutChangedEventArgs("Dvorak", true, isInitial: true);

        // Act - Simulate OnLayoutChanged logic
        var shouldShowBalloon = !layoutArgs.IsInitial;

        // Assert
        shouldShowBalloon.ShouldBeFalse();
    }

    [Fact]
    public void BalloonNotification_ShouldShowOnLayoutChange()
    {
        // Arrange
        var layoutArgs = new LayoutChangedEventArgs("AZERTY", false, isInitial: false);

        // Act - Simulate OnLayoutChanged logic
        var shouldShowBalloon = !layoutArgs.IsInitial;

        // Assert
        shouldShowBalloon.ShouldBeTrue();
    }

    #endregion

    #region Log Path Tests

    [Fact]
    public void LogPath_ShouldBeUnderCommonApplicationData()
    {
        // Arrange & Act
        var logPath = Path.Combine(
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
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "KeyboardAutoSwitcher",
            "logs");

        // Assert
        Path.IsPathRooted(logPath).ShouldBeTrue();
    }

    #endregion
}
