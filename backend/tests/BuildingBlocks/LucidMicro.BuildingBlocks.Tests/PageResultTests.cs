using LucidMicro.BuildingBlocks.Persistence.Abstractions.Models;

namespace LucidMicro.BuildingBlocks.Tests;

public sealed class PageResultTests
{
    [Fact]
    public void Map_TransformsItems_AndPreservesPagingMetadata()
    {
        var page = new PageResult<int>([1, 2, 3], totalCount: 10, pageNumber: 2, pageSize: 3);

        var mapped = page.Map(value => $"item-{value}");

        Assert.Equal(["item-1", "item-2", "item-3"], mapped.Items);
        Assert.Equal(10, mapped.TotalCount);
        Assert.Equal(2, mapped.PageNumber);
        Assert.Equal(3, mapped.PageSize);
        Assert.Equal(4, mapped.TotalPages);
        Assert.True(mapped.HasPreviousPage);
        Assert.True(mapped.HasNextPage);
    }
}
