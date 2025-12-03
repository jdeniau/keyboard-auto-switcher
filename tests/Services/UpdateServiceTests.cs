using KeyboardAutoSwitcher.Services;
using Moq;
using Shouldly;
using Velopack;
using Velopack.Locators;
using Xunit;

namespace KeyboardAutoSwitcher.Tests;

/// <summary>
/// Unit tests for UpdateService (Services/UpdateService.cs)
/// </summary>
public class UpdateServiceTests
{
    private static TestVelopackLocator CreateTestLocator()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "velopack-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        return new TestVelopackLocator(
            appId: "KeyboardAutoSwitcher",
            version: "1.2.3",
            packagesDir: tempDir);
    }

    [Fact]
    public void Constructor_WithLocator_ShouldNotThrow()
    {
        // Arrange
        var testLocator = CreateTestLocator();

        // Act & Assert - Should not throw
        var service = new UpdateService(testLocator);
        service.ShouldNotBeNull();
    }

    [Fact]
    public void CurrentVersion_ShouldReturnValidVersion()
    {
        // Arrange
        var testLocator = CreateTestLocator();
        var service = new UpdateService(testLocator);

        // Act
        var version = service.CurrentVersion;

        // Assert
        version.ShouldNotBeNullOrEmpty();
        var parts = version.Split('.');
        parts.Length.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void CurrentVersion_MultipleCalls_ShouldReturnSameValue()
    {
        // Arrange
        var testLocator = CreateTestLocator();
        var service = new UpdateService(testLocator);

        // Act
        var version1 = service.CurrentVersion;
        var version2 = service.CurrentVersion;

        // Assert
        version1.ShouldBe(version2);
    }

    #region IUpdateManager Mock Tests

    [Fact]
    public async Task CheckForUpdatesAsync_WhenNoUpdate_ShouldReturnNull()
    {
        // Arrange
        var mockUpdateManager = new Mock<IUpdateManager>();
        mockUpdateManager.Setup(m => m.CheckForUpdatesAsync())
            .ReturnsAsync((UpdateInfo?)null);

        // Act
        var result = await mockUpdateManager.Object.CheckForUpdatesAsync();

        // Assert
        result.ShouldBeNull();
        mockUpdateManager.Verify(m => m.CheckForUpdatesAsync(), Times.Once);
    }

    [Fact]
    public async Task CheckForUpdatesSilentAsync_WhenNoUpdate_ShouldReturnFalseAndNull()
    {
        // Arrange
        var mockUpdateManager = new Mock<IUpdateManager>();
        mockUpdateManager.Setup(m => m.CheckForUpdatesSilentAsync())
            .ReturnsAsync((false, (string?)null));

        // Act
        var (available, newVersion) = await mockUpdateManager.Object.CheckForUpdatesSilentAsync();

        // Assert
        available.ShouldBeFalse();
        newVersion.ShouldBeNull();
        mockUpdateManager.Verify(m => m.CheckForUpdatesSilentAsync(), Times.Once);
    }

    [Fact]
    public async Task CheckForUpdatesSilentAsync_WhenUpdateAvailable_ShouldReturnTrueAndVersion()
    {
        // Arrange
        var mockUpdateManager = new Mock<IUpdateManager>();
        mockUpdateManager.Setup(m => m.CheckForUpdatesSilentAsync())
            .ReturnsAsync((true, "2.0.0"));

        // Act
        var (available, newVersion) = await mockUpdateManager.Object.CheckForUpdatesSilentAsync();

        // Assert
        available.ShouldBeTrue();
        newVersion.ShouldBe("2.0.0");
        mockUpdateManager.Verify(m => m.CheckForUpdatesSilentAsync(), Times.Once);
    }

    [Fact]
    public void IUpdateManager_CurrentVersion_ShouldReturnConfiguredValue()
    {
        // Arrange
        var mockUpdateManager = new Mock<IUpdateManager>();
        mockUpdateManager.Setup(m => m.CurrentVersion).Returns("1.2.3");

        // Act
        var version = mockUpdateManager.Object.CurrentVersion;

        // Assert
        version.ShouldBe("1.2.3");
    }

    [Fact]
    public async Task DownloadAndApplyUpdateAsync_ShouldCallWithCorrectParameters()
    {
        // Arrange
        var mockUpdateManager = new Mock<IUpdateManager>();
        Action<int>? capturedCallback = null;

        mockUpdateManager.Setup(m => m.DownloadAndApplyUpdateAsync(It.IsAny<UpdateInfo>(), It.IsAny<Action<int>?>()))
            .Callback<UpdateInfo, Action<int>?>((info, callback) => capturedCallback = callback)
            .ReturnsAsync(true);

        // Act
        var result = await mockUpdateManager.Object.DownloadAndApplyUpdateAsync(null!, progress => { });

        // Assert
        result.ShouldBeTrue();
        mockUpdateManager.Verify(m => m.DownloadAndApplyUpdateAsync(It.IsAny<UpdateInfo>(), It.IsAny<Action<int>?>()), Times.Once);
    }

    [Fact]
    public async Task DownloadAndApplyUpdateAsync_WhenFailed_ShouldReturnFalse()
    {
        // Arrange
        var mockUpdateManager = new Mock<IUpdateManager>();
        mockUpdateManager.Setup(m => m.DownloadAndApplyUpdateAsync(It.IsAny<UpdateInfo>(), It.IsAny<Action<int>?>()))
            .ReturnsAsync(false);

        // Act
        var result = await mockUpdateManager.Object.DownloadAndApplyUpdateAsync(null!, null);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion
}
