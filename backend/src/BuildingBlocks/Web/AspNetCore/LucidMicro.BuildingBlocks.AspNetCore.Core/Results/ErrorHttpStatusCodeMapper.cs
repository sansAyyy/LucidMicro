using LucidMicro.BuildingBlocks.Application.Results;
using Microsoft.AspNetCore.Http;

namespace LucidMicro.BuildingBlocks.AspNetCore.Results;

internal static class ErrorHttpStatusCodeMapper
{
    public static int Map(ErrorType errorType)
    {
        return errorType switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}
