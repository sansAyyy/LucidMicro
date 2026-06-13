using LucidMicro.BuildingBlocks.Application.Results;
using LucidMicro.BuildingBlocks.AspNetCore.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LucidMicro.BuildingBlocks.Tests.AspNetCore;

public sealed class ResultProblemDetailsMappingTests
{
    [Theory]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorType.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ErrorType.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ErrorType.Failure, StatusCodes.Status500InternalServerError)]
    [InlineData(ErrorType.None, StatusCodes.Status500InternalServerError)]
    public void ErrorHttpStatusCodeMapper_MapsErrorTypeToHttpStatusCode(
        ErrorType errorType,
        int expectedStatusCode)
    {
        var statusCode = ErrorHttpStatusCodeMapper.Map(errorType);

        Assert.Equal(expectedStatusCode, statusCode);
    }

    [Fact]
    public void ErrorProblemDetailsFactory_CreatesProblemDetailsFromError()
    {
        var error = Error.Conflict("Identity.AdminUsers.EmailConflict", "Admin user email already exists.");
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "test-trace-id"
        };

        var problemDetails = ErrorProblemDetailsFactory.Create(
            error,
            StatusCodes.Status409Conflict,
            httpContext);

        Assert.Equal(StatusCodes.Status409Conflict, problemDetails.Status);
        Assert.Equal("Admin user email already exists.", problemDetails.Title);
        Assert.Null(problemDetails.Detail);
        Assert.Equal("Identity.AdminUsers.EmailConflict", problemDetails.Extensions["code"]);
        Assert.Equal(nameof(ErrorType.Conflict), problemDetails.Extensions["errorType"]);
        Assert.Equal("test-trace-id", problemDetails.Extensions["traceId"]);
    }

    [Fact]
    public void ToActionResult_ReturnsSuccessActionResult_WhenResultIsSuccess()
    {
        var controller = new TestController();
        var expectedResult = new NoContentResult();

        var actionResult = controller.ToActionResult(Result.Success(), () => expectedResult);

        Assert.Same(expectedResult, actionResult);
    }

    [Fact]
    public void ToActionResult_ReturnsProblemDetails_WhenResultIsFailure()
    {
        var controller = new TestController();
        var result = Result.Failure(Error.NotFound("Resource.NotFound", "Resource was not found."));

        var actionResult = controller.ToActionResult(result, () => new NoContentResult());

        var objectResult = Assert.IsType<ObjectResult>(actionResult);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
        Assert.Equal(StatusCodes.Status404NotFound, problemDetails.Status);
        Assert.Equal("Resource was not found.", problemDetails.Title);
        Assert.Equal("Resource.NotFound", problemDetails.Extensions["code"]);
        Assert.Equal(nameof(ErrorType.NotFound), problemDetails.Extensions["errorType"]);
        Assert.NotNull(problemDetails.Extensions["traceId"]);
    }

    [Fact]
    public void ToActionResultOfT_ReturnsOkObjectResult_WhenResultIsSuccessAndNoSuccessFactoryIsProvided()
    {
        var controller = new TestController();
        var value = new TestResponse("admin");

        var actionResult = controller.ToActionResult(Result<TestResponse>.Success(value));

        var okObjectResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal(StatusCodes.Status200OK, okObjectResult.StatusCode);
        Assert.Same(value, okObjectResult.Value);
    }

    [Fact]
    public void ToActionResultOfT_ReturnsCustomSuccessActionResult_WhenSuccessFactoryIsProvided()
    {
        var controller = new TestController();
        var value = new TestResponse("admin");

        var actionResult = controller.ToActionResult(
            Result<TestResponse>.Success(value),
            response => new CreatedResult($"/admin-users/{response.Name}", response));

        var createdResult = Assert.IsType<CreatedResult>(actionResult.Result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.Equal("/admin-users/admin", createdResult.Location);
        Assert.Same(value, createdResult.Value);
    }

    [Fact]
    public void ToActionResultOfT_ReturnsProblemDetails_WhenResultIsFailure()
    {
        var controller = new TestController();
        var result = Result<TestResponse>.Failure(Error.Validation("Request.Invalid", "Request is invalid."));

        var actionResult = controller.ToActionResult(result);

        var objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
        var problemDetails = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        Assert.Equal(StatusCodes.Status400BadRequest, problemDetails.Status);
        Assert.Equal("Request is invalid.", problemDetails.Title);
        Assert.Equal("Request.Invalid", problemDetails.Extensions["code"]);
        Assert.Equal(nameof(ErrorType.Validation), problemDetails.Extensions["errorType"]);
        Assert.NotNull(problemDetails.Extensions["traceId"]);
    }

    private sealed class TestController : ControllerBase
    {
        public TestController()
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    TraceIdentifier = "test-controller-trace-id"
                }
            };
        }
    }

    private sealed record TestResponse(string Name);
}
