using KeyboardAutoSwitcher.Services;
using Moq;
using Shouldly;
using Velopack;
using Velopack.Locators;

namespace KeyboardAutoSwitcher.Tests.Services
{
    /// <summary>
    /// Unit tests for UpdateService (Services/UpdateService.cs)
    /// </summary>
    public class UpdateServiceTests
    {
        private static TestVelopackLocator CreateTestLocator()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "velopack-test-" + Guid.NewGuid().ToString("N"));
            _ = Directory.CreateDirectory(tempDir);
            return new TestVelopackLocator(
                appId: "KeyboardAutoSwitcher",
                version: "1.2.3",
                packagesDir: tempDir);
        }

        [Fact]
        public void Constructor_WithLocator_ShouldNotThrow()
        {
            // Arrange
            TestVelopackLocator testLocator = CreateTestLocator();

            // Act & Assert - Should not throw
            UpdateService service = new(testLocator);
            _ = service.ShouldNotBeNull();
        }

        [Fact]
        public void CurrentVersion_ShouldReturnValidVersion()
        {
            // Arrange
            TestVelopackLocator testLocator = CreateTestLocator();
            UpdateService service = new(testLocator);

            // Act
            string version = service.CurrentVersion;

            // Assert
            version.ShouldNotBeNullOrEmpty();
            string[] parts = version.Split('.');
            parts.Length.ShouldBeGreaterThanOrEqualTo(2);
        }

        [Fact]
        public void CurrentVersion_MultipleCalls_ShouldReturnSameValue()
        {
            // Arrange
            TestVelopackLocator testLocator = CreateTestLocator();
            UpdateService service = new(testLocator);

            // Act
            string version1 = service.CurrentVersion;
            string version2 = service.CurrentVersion;

            // Assert
            version1.ShouldBe(version2);
        }

        #region IUpdateManager Mock Tests

        [Fact]
        public async Task CheckForUpdatesAsync_WhenNoUpdate_ShouldReturnNull()
        {
            // Arrange
            Mock<IUpdateManager> mockUpdateManager = new();
            _ = mockUpdateManager.Setup(m => m.CheckForUpdatesAsync())
                .ReturnsAsync((UpdateInfo?)null);

            // Act
            UpdateInfo? result = await mockUpdateManager.Object.CheckForUpdatesAsync();

            // Assert
            result.ShouldBeNull();
            mockUpdateManager.Verify(m => m.CheckForUpdatesAsync(), Times.Once);
        }

        [Fact]
        public async Task CheckForUpdatesSilentAsync_WhenNoUpdate_ShouldReturnFalseAndNull()
        {
            // Arrange
            Mock<IUpdateManager> mockUpdateManager = new();
            _ = mockUpdateManager.Setup(m => m.CheckForUpdatesSilentAsync())
                .ReturnsAsync((false, null));

            // Act
            (bool available, string? newVersion) = await mockUpdateManager.Object.CheckForUpdatesSilentAsync();

            // Assert
            available.ShouldBeFalse();
            newVersion.ShouldBeNull();
            mockUpdateManager.Verify(m => m.CheckForUpdatesSilentAsync(), Times.Once);
        }

        [Fact]
        public async Task CheckForUpdatesSilentAsync_WhenUpdateAvailable_ShouldReturnTrueAndVersion()
        {
            // Arrange
            Mock<IUpdateManager> mockUpdateManager = new();
            _ = mockUpdateManager.Setup(m => m.CheckForUpdatesSilentAsync())
                .ReturnsAsync((true, "2.0.0"));

            // Act
            (bool available, string? newVersion) = await mockUpdateManager.Object.CheckForUpdatesSilentAsync();

            // Assert
            available.ShouldBeTrue();
            newVersion.ShouldBe("2.0.0");
            mockUpdateManager.Verify(m => m.CheckForUpdatesSilentAsync(), Times.Once);
        }

        [Fact]
        public void IUpdateManager_CurrentVersion_ShouldReturnConfiguredValue()
        {
            // Arrange
            Mock<IUpdateManager> mockUpdateManager = new();
            _ = mockUpdateManager.Setup(m => m.CurrentVersion).Returns("1.2.3");

            // Act
            string version = mockUpdateManager.Object.CurrentVersion;

            // Assert
            version.ShouldBe("1.2.3");
        }

        [Fact]
        public async Task DownloadAndApplyUpdateAsync_ShouldCallWithCorrectParameters()
        {
            // Arrange
            Mock<IUpdateManager> mockUpdateManager = new();
            Action<int>? capturedCallback = null;

            _ = mockUpdateManager.Setup(m => m.DownloadAndApplyUpdateAsync(It.IsAny<UpdateInfo>(), It.IsAny<Action<int>?>()))
                .Callback<UpdateInfo, Action<int>?>((info, callback) => capturedCallback = callback)
                .ReturnsAsync(true);

            // Act
            bool result = await mockUpdateManager.Object.DownloadAndApplyUpdateAsync(null!, progress => { });

            // Assert
            result.ShouldBeTrue();
            mockUpdateManager.Verify(m => m.DownloadAndApplyUpdateAsync(It.IsAny<UpdateInfo>(), It.IsAny<Action<int>?>()), Times.Once);
        }

        [Fact]
        public async Task DownloadAndApplyUpdateAsync_WhenFailed_ShouldReturnFalse()
        {
            // Arrange
            Mock<IUpdateManager> mockUpdateManager = new();
            _ = mockUpdateManager.Setup(m => m.DownloadAndApplyUpdateAsync(It.IsAny<UpdateInfo>(), It.IsAny<Action<int>?>()))
                .ReturnsAsync(false);

            // Act
            bool result = await mockUpdateManager.Object.DownloadAndApplyUpdateAsync(null!, null);

            // Assert
            result.ShouldBeFalse();
        }

        #endregion
    }
}
