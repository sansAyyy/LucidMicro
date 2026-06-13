using LucidMicro.BuildingBlocks.Application.Results;
using Microsoft.AspNetCore.Mvc;

namespace LucidMicro.BuildingBlocks.AspNetCore.Results;

public static class ControllerResultExtensions
{
    public static ActionResult ToActionResult(
        this ControllerBase controller,
        Result result,
        Func<ActionResult> onSuccess)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);

        if (result.IsSuccess)
        {
            return onSuccess();
        }

        return controller.ToFailureActionResult(result.Error);
    }

    public static ActionResult<T> ToActionResult<T>(
        this ControllerBase controller,
        Result<T> result,
        Func<T, ActionResult>? onSuccess = null)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
        {
            return onSuccess is null
                ? controller.Ok(result.Value)
                : onSuccess(result.Value);
        }

        return controller.ToFailureActionResult(result.Error);
    }

    private static ActionResult ToFailureActionResult(this ControllerBase controller, Error error)
    {
        var statusCode = ErrorHttpStatusCodeMapper.Map(error.Type);
        var problemDetails = ErrorProblemDetailsFactory.Create(error, statusCode, controller.HttpContext);

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode
        };
    }
}
