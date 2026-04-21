using MQControlador.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace MQControlador.Tests;

public class SchedulerHelperTests
{
    private readonly Mock<ILogger<SchedulerHelper>> _mockLogger;
    private readonly SchedulerHelper _schedulerHelper;

    public SchedulerHelperTests()
    {
        _mockLogger = new Mock<ILogger<SchedulerHelper>>();
        _schedulerHelper = new SchedulerHelper(_mockLogger.Object);
    }

    [Fact]
    public void ParseScheduledTime_ValidFormat_ReturnsTimeSpan()
    {
        // Arrange
        var timeString = "14:30";

        // Act
        var result = _schedulerHelper.ParseScheduledTime(timeString);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(new TimeSpan(14, 30, 0), result);
    }

    [Fact]
    public void ParseScheduledTime_InvalidFormat_ReturnsNull()
    {
        // Arrange
        var timeString = "invalid";

        // Act
        var result = _schedulerHelper.ParseScheduledTime(timeString);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseScheduledTime_EmptyString_ReturnsNull()
    {
        // Arrange
        var timeString = "";

        // Act
        var result = _schedulerHelper.ParseScheduledTime(timeString);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetTimeUntilNextExecution_ReturnsFutureTimeSpan()
    {
        // Arrange
        var now = DateTime.Now;
        var futureTime = now.AddHours(2).TimeOfDay;

        // Act
        var result = _schedulerHelper.GetTimeUntilNextExecution(futureTime);

        // Assert
        Assert.True(result.TotalMinutes > 100); // Deve ser aproximadamente 2 horas (ou mais se passou pouco tempo)
    }
}

public class ServiceManagerTests
{
    private readonly Mock<ILogger<ServiceManager>> _mockLogger;
    private readonly ServiceManager _serviceManager;

    public ServiceManagerTests()
    {
        _mockLogger = new Mock<ILogger<ServiceManager>>();
        _serviceManager = new ServiceManager(_mockLogger.Object);
    }

    [Fact]
    public void ReadServicesFromFile_FileNotFound_ReturnsEmptyList()
    {
        // Arrange
        var filePath = "/nonexistent/path/services.txt";

        // Act
        var result = _serviceManager.ReadServicesFromFile(filePath);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ReadServicesFromFile_ValidFile_ReturnsServices()
    {
        // Arrange
        var testFilePath = Path.Combine(Path.GetTempPath(), "test_services.txt");
        File.WriteAllLines(testFilePath, new[] { "Service1", "Service2", "Service3" });

        try
        {
            // Act
            var result = _serviceManager.ReadServicesFromFile(testFilePath);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Contains("Service1", result);
            Assert.Contains("Service2", result);
            Assert.Contains("Service3", result);
        }
        finally
        {
            File.Delete(testFilePath);
        }
    }

    [Fact]
    public void ReadServicesFromFile_FileWithEmptyLines_IgnoresEmpty()
    {
        // Arrange
        var testFilePath = Path.Combine(Path.GetTempPath(), "test_services_empty.txt");
        File.WriteAllLines(testFilePath, new[] { "Service1", "", "Service2", "  ", "Service3" });

        try
        {
            // Act
            var result = _serviceManager.ReadServicesFromFile(testFilePath);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.DoesNotContain("", result);
        }
        finally
        {
            File.Delete(testFilePath);
        }
    }

    [Fact]
    public void ReadServicesFromFile_EmptyString_ReturnsEmpty()
    {
        // Arrange
        var filePath = "";

        // Act
        var result = _serviceManager.ReadServicesFromFile(filePath);

        // Assert
        Assert.Empty(result);
    }
}

public class ServiceRestarterTests
{
    private readonly Mock<ILogger<ServiceRestarter>> _mockLogger;
    private readonly ServiceRestarter _serviceRestarter;

    public ServiceRestarterTests()
    {
        _mockLogger = new Mock<ILogger<ServiceRestarter>>();
        _serviceRestarter = new ServiceRestarter(_mockLogger.Object);
    }

    [Fact]
    public async Task RestartServiceAsync_ServiceNotFound_ReturnsFailed()
    {
        // Arrange
        var serviceName = "NonexistentService12345";

        // Act
        var result = await _serviceRestarter.RestartServiceAsync(serviceName);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Equal(serviceName, result.ServiceName);
    }

    [Fact]
    public async Task RestartServiceAsync_PopulatesTimestamps()
    {
        // Arrange
        var serviceName = "NonexistentService";

        // Act
        var result = await _serviceRestarter.RestartServiceAsync(serviceName);

        // Assert
        Assert.NotEqual(DateTime.MinValue, result.StartTime);
        Assert.NotEqual(DateTime.MinValue, result.EndTime);
        Assert.True(result.EndTime >= result.StartTime);
    }
}

