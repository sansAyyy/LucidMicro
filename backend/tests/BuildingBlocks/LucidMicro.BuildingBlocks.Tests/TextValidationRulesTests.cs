using LucidMicro.BuildingBlocks.Application.Validation;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class TextValidationRulesTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    [InlineData("value", true)]
    public void BeRequired_ReturnsExpectedResult(string? value, bool expected)
    {
        Assert.Equal(expected, TextValidationRules.BeRequired(value));
    }

    [Theory]
    [InlineData(null, 3, true)]
    [InlineData(" abc ", 3, true)]
    [InlineData(" abcd ", 3, false)]
    public void HaveTrimmedMaxLength_ReturnsExpectedResult(string? value, int maxLength, bool expected)
    {
        Assert.Equal(expected, TextValidationRules.HaveTrimmedMaxLength(value, maxLength));
    }

    [Theory]
    [InlineData(null, 3, true)]
    [InlineData(" ", 3, true)]
    [InlineData(" abc ", 3, true)]
    [InlineData(" abcd ", 3, false)]
    public void BeOptionalWithTrimmedMaxLength_ReturnsExpectedResult(string? value, int maxLength, bool expected)
    {
        Assert.Equal(expected, TextValidationRules.BeOptionalWithTrimmedMaxLength(value, maxLength));
    }
}
