using LucidMicro.BuildingBlocks.Domain.Core.Exceptions;
using LucidMicro.BuildingBlocks.Domain.Core.Guards;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class DomainGuardTests
{
    [Fact]
    public void RequiredText_ReturnsTrimmedValue_WhenValueIsValid()
    {
        var value = DomainGuard.RequiredText(" name ", "name", 10);

        Assert.Equal("name", value);
    }

    [Fact]
    public void RequiredText_ThrowsDomainException_WhenValueIsBlank()
    {
        var exception = Assert.Throws<DomainException>(
            () => DomainGuard.RequiredText(" ", "name", 10));

        Assert.Equal("name is required.", exception.Message);
    }

    [Fact]
    public void RequiredText_ThrowsDomainException_WhenTrimmedValueExceedsMaxLength()
    {
        var exception = Assert.Throws<DomainException>(
            () => DomainGuard.RequiredText("abcd", "name", 3));

        Assert.Equal("name exceeds max length 3.", exception.Message);
    }

    [Fact]
    public void OptionalText_ReturnsNull_WhenValueIsBlank()
    {
        var value = DomainGuard.OptionalText(" ", "phoneNumber", 10);

        Assert.Null(value);
    }

    [Fact]
    public void OptionalText_ReturnsTrimmedValue_WhenValueIsPresent()
    {
        var value = DomainGuard.OptionalText(" 123 ", "phoneNumber", 10);

        Assert.Equal("123", value);
    }
}
