using System.Reflection;
using Either.SampleApi.Features.Payments.Refund;
using Either.SampleApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Either.SampleApi.Tests;

public sealed class RefundPaymentEndpointTest
{
    [Fact(DisplayName = "HandleAsync returns status 499 when request is canceled")]
    public async Task HandleAsyncReturns499WhenCanceled()
    {
        MethodInfo? handleAsyncMethod = typeof(RefundPaymentEndpoint).GetMethod("HandleAsync", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(handleAsyncMethod);

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"Either.SampleApi.Tests.{Guid.NewGuid():N}")
            .Options;

        await using AppDbContext dbContext = new(options);
        using CancellationTokenSource cancellationTokenSource = new();
        cancellationTokenSource.Cancel();

        object? rawResult = handleAsyncMethod.Invoke(
            null,
            [new RefundPaymentRequest(Guid.NewGuid(), 10.00m, "Customer cancellation"), dbContext, cancellationTokenSource.Token]);

        Task<IResult>? resultTask = rawResult as Task<IResult>;
        Assert.NotNull(resultTask);
        IResult result = await resultTask;
        IStatusCodeHttpResult statusCodeResult = Assert.IsType<IStatusCodeHttpResult>(result, exactMatch: false);
        Assert.Equal(499, statusCodeResult.StatusCode);
    }
}
