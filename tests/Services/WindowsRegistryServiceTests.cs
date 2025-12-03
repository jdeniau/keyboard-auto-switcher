using KeyboardAutoSwitcher.Services;
using Shouldly;
using Xunit;

namespace KeyboardAutoSwitcher.Tests.Services;

/// <summary>
/// Unit tests for WindowsRegistryService (Services/IRegistryService.cs)
/// Note: These are integration tests that actually access the Windows Registry
/// </summary>
public class WindowsRegistryServiceTests
{
    private const string TestKeyPath = @"Software\KeyboardAutoSwitcherTests";
    private const string TestValueName = "TestValue";

    private readonly WindowsRegistryService _registryService;

    public WindowsRegistryServiceTests()
    {
        _registryService = new WindowsRegistryService();
    }

    #region GetValue Tests

    [Fact]
    public void GetValue_WhenKeyDoesNotExist_ShouldReturnNull()
    {
        // Arrange - Use a key that definitely doesn't exist
        var nonExistentKey = @"Software\NonExistentKeyForTesting12345";

        // Act
        var result = _registryService.GetValue(nonExistentKey, "SomeValue");

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void GetValue_WhenValueDoesNotExist_ShouldReturnNull()
    {
        // Arrange - Use a key that exists but value that doesn't
        var existingKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

        // Act
        var result = _registryService.GetValue(existingKey, "NonExistentValue12345");

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region SetValue Tests

    [Fact]
    public void SetValue_WhenKeyDoesNotExist_ShouldReturnFalse()
    {
        // Arrange - Use a key path that doesn't exist and can't be written to without CreateSubKey
        var nonExistentKey = @"Software\NonExistentKeyForTesting12345\SubKey";

        // Act
        var result = _registryService.SetValue(nonExistentKey, TestValueName, "test");

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region DeleteValue Tests

    [Fact]
    public void DeleteValue_WhenKeyDoesNotExist_ShouldReturnFalse()
    {
        // Arrange - Use a key that definitely doesn't exist
        var nonExistentKey = @"Software\NonExistentKeyForTesting12345";

        // Act
        var result = _registryService.DeleteValue(nonExistentKey, TestValueName);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region Integration Tests (Round-trip)

    [Fact]
    public void SetAndGetValue_RoundTrip_ShouldWork()
    {
        // This test requires creating a test registry key first
        // Skip if we can't create the test environment
        using var testKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(TestKeyPath);
        if (testKey == null)
        {
            return; // Skip test if we can't create the test key
        }

        try
        {
            // Arrange
            var testValue = "TestValue123";

            // Act - Set the value
            var setResult = _registryService.SetValue(TestKeyPath, TestValueName, testValue);

            // Assert - Set should succeed
            setResult.ShouldBeTrue();

            // Act - Get the value back
            var getValue = _registryService.GetValue(TestKeyPath, TestValueName);

            // Assert - Should get the same value back
            getValue.ShouldBe(testValue);
        }
        finally
        {
            // Cleanup
            Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(TestKeyPath, false);
        }
    }

    [Fact]
    public void DeleteValue_AfterSetting_ShouldSucceed()
    {
        // Create test key
        using var testKey = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(TestKeyPath);
        if (testKey == null)
        {
            return; // Skip test if we can't create the test key
        }

        try
        {
            // Arrange - Set a value first
            _registryService.SetValue(TestKeyPath, TestValueName, "ToDelete");

            // Act - Delete the value
            var deleteResult = _registryService.DeleteValue(TestKeyPath, TestValueName);

            // Assert
            deleteResult.ShouldBeTrue();

            // Verify it's gone
            var getValue = _registryService.GetValue(TestKeyPath, TestValueName);
            getValue.ShouldBeNull();
        }
        finally
        {
            // Cleanup
            Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(TestKeyPath, false);
        }
    }

    #endregion
}
