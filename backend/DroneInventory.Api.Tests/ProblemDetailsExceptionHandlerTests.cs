using DroneInventory.Api.Domain;
using DroneInventory.Api.ErrorHandling;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DroneInventory.Api.Tests;

public class ProblemDetailsExceptionHandlerTests
{
    [Theory]
    [InlineData(typeof(ValidationException), StatusCodes.Status400BadRequest)]
    [InlineData(typeof(NotFoundException), StatusCodes.Status404NotFound)]
    [InlineData(typeof(ConflictException), StatusCodes.Status409Conflict)]
    public async Task Domain_exceptions_are_returned_as_problem_details(Type exceptionType, int status)
    {
        var writer = new RecordingProblemDetailsService();
        var handler = new ProblemDetailsExceptionHandler(writer);
        var httpContext = new DefaultHttpContext();
        var exception = (Exception)Activator.CreateInstance(exceptionType, "TELLO-07 is already loaned")!;

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(status, httpContext.Response.StatusCode);
        Assert.Equal(status, writer.ProblemDetails!.Status);
        Assert.Equal("TELLO-07 is already loaned", writer.ProblemDetails.Detail);
    }

    [Fact]
    public async Task Other_exceptions_are_not_handled()
    {
        var writer = new RecordingProblemDetailsService();
        var handler = new ProblemDetailsExceptionHandler(writer);

        var handled = await handler.TryHandleAsync(
            new DefaultHttpContext(),
            new InvalidOperationException("boom"),
            CancellationToken.None);

        Assert.False(handled);
        Assert.Null(writer.ProblemDetails);
    }

    private sealed class RecordingProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetails? ProblemDetails { get; private set; }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            ProblemDetails = context.ProblemDetails;
            return ValueTask.FromResult(true);
        }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            ProblemDetails = context.ProblemDetails;
            return ValueTask.CompletedTask;
        }

        public bool TryWrite(ProblemDetailsContext context) => throw new NotSupportedException();
    }
}
