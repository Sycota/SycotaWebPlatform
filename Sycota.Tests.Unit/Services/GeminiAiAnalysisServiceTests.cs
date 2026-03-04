using System.Reflection;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using Sycota.Application.Services;
using Sycota.Domain.Classes;
using System.Net;
using System.Text.Json;

namespace Sycota.Tests.Unit.Services;

public class GeminiAiAnalysisServiceTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    
    public GeminiAiAnalysisServiceTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _configurationMock.Setup(x => x["Gemini:ApiKey"]).Returns("test-api-key");
        _configurationMock.Setup(x => x["Gemini:ModelName"]).Returns("gemini-2.5-flash");
    }

    #region CalculateScore Tests

    [Theory]
    [InlineData(0, 0, 10.9)]       // Dead center
    [InlineData(0.5, 0, 10.7)]     // Very close to center
    [InlineData(1.0, 0, 10.5)]     // Still in 10 ring
    [InlineData(2.0, 0, 10.2)]     // Near edge of 10 ring
    public void CalculateScore_InnerTenRing_ReturnsCorrectScore(double x, double y, double expectedMinScore)
    {
        // Arrange
        var service = CreateService();
        var method = GetPrivateMethod(service, "CalculateScore");

        // Act
        var result = (double)method.Invoke(service, new object[] { x, y })!;

        // Assert
        Assert.True(result >= expectedMinScore - 0.2, $"Expected score >= {expectedMinScore - 0.2}, got {result}");
        Assert.True(result <= 10.9, $"Expected score <= 10.9, got {result}");
    }

    [Theory]
    [InlineData(5.0, 0)]   // In ring 8 area
    [InlineData(0, 5.0)]   // In ring 8 area (vertical)
    [InlineData(10.0, 0)]  // In ring 6 area
    [InlineData(15.0, 0)]  // In ring 4 area
    public void CalculateScore_OuterRings_ReturnsLowerScores(double x, double y)
    {
        // Arrange
        var service = CreateService();
        var method = GetPrivateMethod(service, "CalculateScore");

        // Act
        var result = (double)method.Invoke(service, new object[] { x, y })!;

        // Assert
        Assert.True(result < 10.0, $"Expected score < 10.0 for position ({x}, {y}), got {result}");
        Assert.True(result >= 0, $"Expected score >= 0, got {result}");
    }

    [Theory]
    [InlineData(25.0, 0)]   // Outside target
    [InlineData(0, 25.0)]   // Outside target
    [InlineData(30.0, 30.0)] // Far outside
    public void CalculateScore_OutsideTarget_ReturnsZero(double x, double y)
    {
        // Arrange
        var service = CreateService();
        var method = GetPrivateMethod(service, "CalculateScore");

        // Act
        var result = (double)method.Invoke(service, new object[] { x, y })!;

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateScore_SymmetricPositions_ReturnSameScore()
    {
        // Arrange
        var service = CreateService();
        var method = GetPrivateMethod(service, "CalculateScore");

        // Act - Test all four quadrants
        var rightScore = (double)method.Invoke(service, new object[] { 3.0, 0.0 })!;
        var leftScore = (double)method.Invoke(service, new object[] { -3.0, 0.0 })!;
        var upScore = (double)method.Invoke(service, new object[] { 0.0, -3.0 })!;
        var downScore = (double)method.Invoke(service, new object[] { 0.0, 3.0 })!;

        // Assert - All should be equal (same distance from center)
        Assert.Equal(rightScore, leftScore);
        Assert.Equal(rightScore, upScore);
        Assert.Equal(rightScore, downScore);
    }

    #endregion

    #region ParseSessionData Tests

    [Fact]
    public void ParseSessionData_EmptyJson_ReturnsNoDataMessage()
    {
        // Arrange
        var service = CreateService();
        var method = GetPrivateMethod(service, "ParseSessionData");

        // Act
        var result = (string)method.Invoke(service, new object[] { "{}" })!;

        // Assert
        Assert.Contains("No shot data available", result);
    }

    [Fact]
    public void ParseSessionData_NullOrWhitespace_ReturnsNoDataMessage()
    {
        // Arrange
        var service = CreateService();
        var method = GetPrivateMethod(service, "ParseSessionData");

        // Act
        var resultNull = (string)method.Invoke(service, new object[] { "" })!;
        var resultWhitespace = (string)method.Invoke(service, new object[] { "   " })!;

        // Assert
        Assert.Contains("No shot data available", resultNull);
        Assert.Contains("No shot data available", resultWhitespace);
    }

    [Fact]
    public void ParseSessionData_ValidWarmupShots_ParsesCorrectly()
    {
        // Arrange
        var service = CreateService();
        var method = GetPrivateMethod(service, "ParseSessionData");
        var json = JsonSerializer.Serialize(new
        {
            warmupShots = new[]
            {
                new { x = 0.0, y = 0.0 },
                new { x = 1.0, y = 1.0 }
            },
            groups = Array.Empty<object>()
        });

        // Act
        var result = (string)method.Invoke(service, new object[] { json })!;

        // Assert
        Assert.Contains("Warmup Shots", result);
        Assert.Contains("2 shots", result);
    }

    [Fact]
    public void ParseSessionData_ValidSeriesGroups_ParsesCorrectly()
    {
        // Arrange
        var service = CreateService();
        var method = GetPrivateMethod(service, "ParseSessionData");
        var json = JsonSerializer.Serialize(new
        {
            warmupShots = Array.Empty<object>(),
            groups = new[]
            {
                new
                {
                    groupId = 1,
                    valueType = "10-shot-series",
                    shots = new[]
                    {
                        new { x = 0.0, y = 0.0 },
                        new { x = 0.5, y = 0.5 },
                        new { x = -0.5, y = -0.5 }
                    }
                }
            }
        });

        // Act
        var result = (string)method.Invoke(service, new object[] { json })!;

        // Assert
        Assert.Contains("Series 1", result);
        Assert.Contains("10-shot-series", result);
        Assert.Contains("3 shots", result);
        Assert.Contains("Summary:", result);
        Assert.Contains("Total=", result);
        Assert.Contains("Avg=", result);
        Assert.Contains("Group center:", result);
        Assert.Contains("Max spread:", result);
    }

    [Fact]
    public void ParseSessionData_InvalidJson_ReturnsRawData()
    {
        // Arrange
        var service = CreateService();
        var method = GetPrivateMethod(service, "ParseSessionData");
        var invalidJson = "{ this is not valid json }}}";

        // Act
        var result = (string)method.Invoke(service, new object[] { invalidJson })!;

        // Assert
        Assert.Contains("Raw shot data:", result);
    }

    [Fact]
    public void ParseSessionData_CalculatesGroupStatistics()
    {
        // Arrange
        var service = CreateService();
        var method = GetPrivateMethod(service, "ParseSessionData");
        var json = JsonSerializer.Serialize(new
        {
            warmupShots = Array.Empty<object>(),
            groups = new[]
            {
                new
                {
                    groupId = 1,
                    valueType = "10-shot-series",
                    shots = new[]
                    {
                        new { x = 0.0, y = 0.0 },   // Score ~10.9
                        new { x = 0.0, y = 0.0 },   // Score ~10.9
                        new { x = 0.0, y = 0.0 }    // Score ~10.9
                    }
                }
            }
        });

        // Act
        var result = (string)method.Invoke(service, new object[] { json })!;

        // Assert
        Assert.Contains("10s=3", result);      // All shots are 10s
        Assert.Contains("Inner 10s=3", result); // All shots are inner 10s (>= 10.5)
    }

    #endregion

    #region GetSystemPrompt Tests

    [Fact]
    public void GetSystemPrompt_ContainsISSFExpertise()
    {
        // Arrange
        var service = CreateService();
        var method = GetPrivateMethod(service, "GetSystemPrompt");

        // Act
        var result = (string)method.Invoke(service, Array.Empty<object>())!;

        // Assert
        Assert.Contains("ISSF", result);
        Assert.Contains("shooting coach", result.ToLower());
    }

    [Fact]
    public void GetSystemPrompt_ContainsFormattingInstructions()
    {
        // Arrange
        var service = CreateService();
        var method = GetPrivateMethod(service, "GetSystemPrompt");

        // Act
        var result = (string)method.Invoke(service, Array.Empty<object>())!;

        // Assert
        Assert.Contains("Do NOT use any markdown formatting", result);
        Assert.Contains("plain text only", result.ToLower());
    }

    [Fact]
    public void GetSystemPrompt_ContainsScoringInstructions()
    {
        // Arrange
        var service = CreateService();
        var method = GetPrivateMethod(service, "GetSystemPrompt");

        // Act
        var result = (string)method.Invoke(service, Array.Empty<object>())!;

        // Assert
        Assert.Contains("decimal scoring", result.ToLower());
        Assert.Contains("10.0 to 10.9", result);
        Assert.Contains("inward gauging", result.ToLower());
    }

    [Fact]
    public void GetSystemPrompt_ContainsTechniqueGuidance()
    {
        // Arrange
        var service = CreateService();
        var method = GetPrivateMethod(service, "GetSystemPrompt");

        // Act
        var result = (string)method.Invoke(service, Array.Empty<object>())!;

        // Assert
        Assert.Contains("trigger", result.ToLower());
        Assert.Contains("breathing", result.ToLower());
        Assert.Contains("grip", result.ToLower());
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithoutApiKey_ThrowsException()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["Gemini:ApiKey"]).Returns((string?)null);
        var httpClient = new HttpClient();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new GeminiAiAnalysisService(httpClient, configMock.Object));
    }

    [Fact]
    public void Constructor_WithApiKey_DoesNotThrow()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["Gemini:ApiKey"]).Returns("test-key");
        var httpClient = new HttpClient();

        // Act & Assert
        var exception = Record.Exception(() => new GeminiAiAnalysisService(httpClient, configMock.Object));
        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_WithoutModelName_UsesDefault()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(x => x["Gemini:ApiKey"]).Returns("test-key");
        configMock.Setup(x => x["Gemini:ModelName"]).Returns((string?)null);
        var httpClient = new HttpClient();

        // Act - should not throw
        var service = new GeminiAiAnalysisService(httpClient, configMock.Object);

        // Assert - service created successfully (uses default model)
        Assert.NotNull(service);
    }

    #endregion

    #region Helper Methods

    private GeminiAiAnalysisService CreateService()
    {
        var httpClient = new HttpClient();
        return new GeminiAiAnalysisService(httpClient, _configurationMock.Object);
    }

    private MethodInfo GetPrivateMethod(object obj, string methodName)
    {
        var type = obj.GetType();
        var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        return method ?? throw new InvalidOperationException($"Method {methodName} not found");
    }

    #endregion
}
