using FluentAssertions;
using FruitShop.Api.Middleware;
using FruitShop.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace FruitShop.Api.Tests;

public sealed class GlobalExceptionHandlerTests
{
    public static IEnumerable<object[]> HandledExceptions =>
    [
        [new NotFoundException("Order", 42), StatusCodes.Status404NotFound, "Resource not found", LogLevel.Warning],
        [new ArgumentException("Invalid"), StatusCodes.Status400BadRequest, "Invalid request", LogLevel.Warning],
        [new InvalidOperationException("Conflict"), StatusCodes.Status409Conflict, "Operation cannot be completed", LogLevel.Warning],
        [new Exception("Unexpected"), StatusCodes.Status500InternalServerError, "An unexpected error occurred", LogLevel.Error]
    ];

    [Theory]
    [MemberData(nameof(HandledExceptions))]
    public async Task TryHandleAsync_ShouldWriteExpectedProblemDetails_WhenExceptionIsHandled(
        Exception exception,
        int expectedStatusCode,
        string expectedTitle,
        LogLevel expectedLogLevel)
    {
        // Arrange
        var logger = new Mock<ILogger<GlobalExceptionHandler>>();
        var problemDetailsService = new Mock<IProblemDetailsService>();
        problemDetailsService.Setup(candidate => candidate.TryWriteAsync(It.IsAny<ProblemDetailsContext>()))
            .Returns(new ValueTask<bool>(true));
        var handler = new GlobalExceptionHandler(
            logger.Object,
            problemDetailsService.Object);
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "test-trace-id"
        };
        httpContext.Request.Path = "/api/orders/42";

        // Act
        var handled = await handler.TryHandleAsync(
            httpContext,
            exception,
            CancellationToken.None);

        // Assert
        handled.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(expectedStatusCode);
        problemDetailsService.Verify(candidate => candidate.TryWriteAsync(It.Is<ProblemDetailsContext>(context =>
            context.HttpContext == httpContext &&
            context.Exception == exception &&
            context.ProblemDetails.Status == expectedStatusCode &&
            context.ProblemDetails.Title == expectedTitle &&
            context.ProblemDetails.Instance == "/api/orders/42" &&
            "test-trace-id".Equals(context.ProblemDetails.Extensions["traceId"]))), Times.Once);
        logger.Invocations.Should().ContainSingle(invocation =>
            invocation.Method.Name == nameof(ILogger.Log) &&
            (LogLevel)invocation.Arguments[0] == expectedLogLevel &&
            ReferenceEquals(invocation.Arguments[3], exception));
    }
}