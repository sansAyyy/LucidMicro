using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Scalar.AspNetCore;

namespace LucidMicro.BuildingBlocks.OpenApi.AspNetCore.DependencyInjection;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapLucidOpenApi(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapOpenApi();
        endpoints.MapScalarApiReference();

        return endpoints;
    }
}
