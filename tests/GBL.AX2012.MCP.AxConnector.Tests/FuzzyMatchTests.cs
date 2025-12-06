using Xunit;
using FluentAssertions;
using GBL.AX2012.MCP.AxConnector.Helpers;

namespace GBL.AX2012.MCP.AxConnector.Tests;

public class FuzzyMatchTests
{
    [Fact]
    public void Score_ExactMatch_Returns100()
    {
        var score = FuzzyMatch.Score("Test", "Test");
        score.Should().Be(100);
    }
    
    [Fact]
    public void Score_ExactMatch_CaseInsensitive()
    {
        var score = FuzzyMatch.Score("TEST", "test");
        score.Should().Be(100);
    }
    
    [Fact]
    public void Score_Contains_ReturnsHighScore()
    {
        var score = FuzzyMatch.Score("Test", "Test Customer Inc");
        score.Should().BeGreaterThan(70);
    }
    
    [Fact]
    public void Score_StartsWith_ReturnsHighScore()
    {
        var score = FuzzyMatch.Score("Test", "Testing Corp");
        score.Should().BeGreaterThan(80);
    }
    
    [Fact]
    public void Score_Similar_ReturnsModerateScore()
    {
        var score = FuzzyMatch.Score("Tset", "Test"); // Typo
        score.Should().BeGreaterThanOrEqualTo(50);
    }
    
    [Fact]
    public void Score_Unrelated_ReturnsLowScore()
    {
        var score = FuzzyMatch.Score("ABC", "XYZ");
        score.Should().BeLessThan(50);
    }
    
    [Fact]
    public void Score_EmptyInput_ReturnsZero()
    {
        var score = FuzzyMatch.Score("", "Test");
        score.Should().Be(0);
    }
    
    [Fact]
    public void Score_EmptyTarget_ReturnsZero()
    {
        var score = FuzzyMatch.Score("Test", "");
        score.Should().Be(0);
    }
}
