using KeyboardAutoSwitcher.Services;
using Moq;
using Shouldly;
using Xunit;

namespace KeyboardAutoSwitcher.Tests;

/// <summary>
/// Unit tests for StartupManager (Services/StartupManager.cs)
/// Uses mocked IRegistryService for isolated testing
/// </summary>
public class StartupManagerTests
{
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "KeyboardAutoSwitcher";

    private readonly Mock<IRegistryService> _mockRegistry;
    private readonly StartupManager _startupManager;

    public StartupManagerTests()
    {
        _mockRegistry = new Mock<IRegistryService>();
        _startupManager = new StartupManager(_mockRegistry.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullRegistryService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => new StartupManager(null!));
    }

    #endregion

    #region IsStartupEnabled Tests

    [Fact]
    public void IsStartupEnabled_WhenRegistryValueIsNull_ShouldReturnFalse()
    {
        // Arrange
        _mockRegistry.Setup(r => r.GetValue(RegistryKeyPath, AppName)).Returns((object?)null);

        // Act
        var result = _startupManager.IsStartupEnabled;

        // Assert
        result.ShouldBeFalse();
        _mockRegistry.Verify(r => r.GetValue(RegistryKeyPath, AppName), Times.Once);
    }

    [Fact]
    public void IsStartupEnabled_WhenRegistryValueMatchesCurrentPath_ShouldReturnTrue()
    {
        // Arrange - The path should match the current executable
        var currentPath = Environment.ProcessPath ?? "test.exe";
        _mockRegistry.Setup(r => r.GetValue(RegistryKeyPath, AppName)).Returns($"\"{currentPath}\"");

        // Act
        var result = _startupManager.IsStartupEnabled;

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsStartupEnabled_WhenRegistryValueDoesNotMatch_ShouldReturnFalse()
    {
        // Arrange - Set a different path
        _mockRegistry.Setup(r => r.GetValue(RegistryKeyPath, AppName)).Returns("\"C:\\some\\other\\path.exe\"");

        // Act
        var result = _startupManager.IsStartupEnabled;

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsStartupEnabled_WhenRegistryThrows_ShouldReturnFalse()
    {
        // Arrange
        _mockRegistry.Setup(r => r.GetValue(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new Exception("Registry error"));

        // Act
        var result = _startupManager.IsStartupEnabled;

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsStartupEnabled_MultipleCalls_ShouldBeConsistent()
    {
        // Arrange
        _mockRegistry.Setup(r => r.GetValue(RegistryKeyPath, AppName)).Returns((object?)null);

        // Act
        var result1 = _startupManager.IsStartupEnabled;
        var result2 = _startupManager.IsStartupEnabled;

        // Assert
        result1.ShouldBe(result2);
    }

    #endregion

    #region EnableStartup Tests

    [Fact]
    public void EnableStartup_WhenRegistrySetSucceeds_ShouldReturnTrue()
    {
        // Arrange
        _mockRegistry.Setup(r => r.SetValue(RegistryKeyPath, AppName, It.IsAny<string>())).Returns(true);

        // Act
        var result = _startupManager.EnableStartup();

        // Assert
        result.ShouldBeTrue();
        _mockRegistry.Verify(r => r.SetValue(RegistryKeyPath, AppName, It.Is<string>(s => s.StartsWith("\"") && s.EndsWith("\""))), Times.Once);
    }

    [Fact]
    public void EnableStartup_WhenRegistrySetFails_ShouldReturnFalse()
    {
        // Arrange
        _mockRegistry.Setup(r => r.SetValue(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>())).Returns(false);

        // Act
        var result = _startupManager.EnableStartup();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void EnableStartup_WhenRegistryThrows_ShouldReturnFalse()
    {
        // Arrange
        _mockRegistry.Setup(r => r.SetValue(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
            .Throws(new Exception("Registry error"));

        // Act
        var result = _startupManager.EnableStartup();

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region DisableStartup Tests

    [Fact]
    public void DisableStartup_WhenRegistryDeleteSucceeds_ShouldReturnTrue()
    {
        // Arrange
        _mockRegistry.Setup(r => r.DeleteValue(RegistryKeyPath, AppName)).Returns(true);

        // Act
        var result = _startupManager.DisableStartup();

        // Assert
        result.ShouldBeTrue();
        _mockRegistry.Verify(r => r.DeleteValue(RegistryKeyPath, AppName), Times.Once);
    }

    [Fact]
    public void DisableStartup_WhenRegistryDeleteFails_ShouldReturnFalse()
    {
        // Arrange
        _mockRegistry.Setup(r => r.DeleteValue(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        // Act
        var result = _startupManager.DisableStartup();

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void DisableStartup_WhenRegistryThrows_ShouldReturnFalse()
    {
        // Arrange
        _mockRegistry.Setup(r => r.DeleteValue(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new Exception("Registry error"));

        // Act
        var result = _startupManager.DisableStartup();

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region ToggleStartup Tests

    [Fact]
    public void ToggleStartup_WhenStartupEnabled_ShouldCallDisable()
    {
        // Arrange - Startup is enabled (current path matches)
        var currentPath = Environment.ProcessPath ?? "test.exe";
        _mockRegistry.Setup(r => r.GetValue(RegistryKeyPath, AppName)).Returns($"\"{currentPath}\"");
        _mockRegistry.Setup(r => r.DeleteValue(RegistryKeyPath, AppName)).Returns(true);

        // Act
        var result = _startupManager.ToggleStartup();

        // Assert
        result.ShouldBeTrue();
        _mockRegistry.Verify(r => r.DeleteValue(RegistryKeyPath, AppName), Times.Once);
        _mockRegistry.Verify(r => r.SetValue(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public void ToggleStartup_WhenStartupDisabled_ShouldCallEnable()
    {
        // Arrange - Startup is disabled
        _mockRegistry.Setup(r => r.GetValue(RegistryKeyPath, AppName)).Returns((object?)null);
        _mockRegistry.Setup(r => r.SetValue(RegistryKeyPath, AppName, It.IsAny<string>())).Returns(true);

        // Act
        var result = _startupManager.ToggleStartup();

        // Assert
        result.ShouldBeTrue();
        _mockRegistry.Verify(r => r.SetValue(RegistryKeyPath, AppName, It.IsAny<string>()), Times.Once);
        _mockRegistry.Verify(r => r.DeleteValue(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    #endregion
}
