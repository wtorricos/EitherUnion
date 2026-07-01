using System.Net;
using System.Text.Json;
using Either.SampleApi.Features.Orders.Create;
using Either.SampleApi.Features.Orders.GetById;
using Either.SampleApi.Features.Payments.Refund;
using Either.SampleApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Either.SampleApi.Tests;

public sealed class SampleApiEndpointsTest
{
    [Fact(DisplayName = "POST /orders returns Created for a valid payload")]
    public async Task CreateOrderReturnsCreatedForValidPayload()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        CreateOrderRequest payload = new("Grace Hopper", 150.00m, "USD");
        HttpResponseMessage response = await client.PostAsJsonAsync("/orders", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        CreateOrderResponse? content = await response.Content.ReadFromJsonAsync<CreateOrderResponse>();
        Assert.NotNull(content);
        Assert.Equal("Grace Hopper", content.CustomerName);
        Assert.Equal(150.00m, content.Amount);
    }

    [Fact(DisplayName = "POST /orders returns ProblemDetails for invalid payload")]
    public async Task CreateOrderReturnsProblemDetailsForInvalidPayload()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        CreateOrderRequest payload = new(string.Empty, 0, "USD");
        HttpResponseMessage response = await client.PostAsJsonAsync("/orders", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonDocument problem = await ReadJsonAsync(response);
        Assert.Equal("VALIDATION_CUSTOMER_NAME", problem.RootElement.GetProperty("title").GetString());
        Assert.Equal(400, problem.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Customer name is required.", problem.RootElement.GetProperty("detail").GetString());
        Assert.Equal("VALIDATION_CUSTOMER_NAME", problem.RootElement.GetProperty("errorCode").GetString());
        Assert.Equal("Warning", problem.RootElement.GetProperty("severity").GetString());

        JsonElement details = problem.RootElement.GetProperty("details");
        Assert.Equal(JsonValueKind.Array, details.ValueKind);
        Assert.True(details.GetArrayLength() > 0);
        Assert.Equal("CUSTOMER_NAME", details[0].GetProperty("code").GetString());
        Assert.Equal("Customer name is required.", details[0].GetProperty("description").GetString());
    }

    [Fact(DisplayName = "POST /orders returns ProblemDetails when amount is not positive")]
    public async Task CreateOrderReturnsProblemDetailsWhenAmountIsNotPositive()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        CreateOrderRequest payload = new("Grace Hopper", 0, "USD");
        HttpResponseMessage response = await client.PostAsJsonAsync("/orders", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonDocument problem = await ReadJsonAsync(response);
        Assert.Equal("VALIDATION_AMOUNT", problem.RootElement.GetProperty("title").GetString());
        Assert.Equal("Order amount must be greater than zero.", problem.RootElement.GetProperty("detail").GetString());
    }

    [Fact(DisplayName = "POST /orders returns ProblemDetails when currency is missing")]
    public async Task CreateOrderReturnsProblemDetailsWhenCurrencyIsMissing()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        CreateOrderRequest payload = new("Grace Hopper", 10.00m, string.Empty);
        HttpResponseMessage response = await client.PostAsJsonAsync("/orders", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonDocument problem = await ReadJsonAsync(response);
        Assert.Equal("VALIDATION_CURRENCY", problem.RootElement.GetProperty("title").GetString());
        Assert.Equal("Currency is required.", problem.RootElement.GetProperty("detail").GetString());
    }

    [Fact(DisplayName = "POST /orders returns ProblemDetails when currency is unsupported")]
    public async Task CreateOrderReturnsProblemDetailsWhenCurrencyIsUnsupported()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        CreateOrderRequest payload = new("Grace Hopper", 10.00m, "BRL");
        HttpResponseMessage response = await client.PostAsJsonAsync("/orders", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonDocument problem = await ReadJsonAsync(response);
        Assert.Equal("VALIDATION_UNSUPPORTED_CURRENCY", problem.RootElement.GetProperty("title").GetString());
        Assert.Equal("The provided currency is not supported for this sample.", problem.RootElement.GetProperty("detail").GetString());
    }

    [Fact(DisplayName = "GET /orders/{id} returns order when it exists")]
    public async Task GetOrderByIdReturnsOrderWhenItExists()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        CreateOrderRequest createPayload = new("Katherine Johnson", 200.00m, "EUR");
        HttpResponseMessage createResponse = await client.PostAsJsonAsync("/orders", createPayload);
        CreateOrderResponse? createdOrder = await createResponse.Content.ReadFromJsonAsync<CreateOrderResponse>();

        Assert.NotNull(createdOrder);
        HttpResponseMessage getResponse = await client.GetAsync($"/orders/{createdOrder.Id:D}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        GetOrderByIdResponse? getContent = await getResponse.Content.ReadFromJsonAsync<GetOrderByIdResponse>();
        Assert.NotNull(getContent);
        Assert.Equal(createdOrder.Id, getContent.Id);
    }

    [Fact(DisplayName = "GET /orders/{id} returns ProblemDetails when missing")]
    public async Task GetOrderByIdReturnsProblemDetailsWhenMissing()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync($"/orders/{Guid.NewGuid():D}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        JsonDocument problem = await ReadJsonAsync(response);
        Assert.Equal("ORDER_NOT_FOUND", problem.RootElement.GetProperty("title").GetString());
        Assert.Equal("ORDER_NOT_FOUND", problem.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact(DisplayName = "POST /payments/refund returns OK for valid refund")]
    public async Task RefundReturnsOkForValidPayload()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        CreateOrderRequest createPayload = new("Dorothy Vaughan", 99.00m, "USD");
        HttpResponseMessage createResponse = await client.PostAsJsonAsync("/orders", createPayload);
        CreateOrderResponse? createdOrder = await createResponse.Content.ReadFromJsonAsync<CreateOrderResponse>();

        Assert.NotNull(createdOrder);
        RefundPaymentRequest refundPayload = new(createdOrder.Id, 25.00m, "Partial reimbursement");
        HttpResponseMessage response = await client.PostAsJsonAsync("/payments/refund", refundPayload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        RefundPaymentResponse? content = await response.Content.ReadFromJsonAsync<RefundPaymentResponse>();
        Assert.NotNull(content);
        Assert.Equal(createdOrder.Id, content.OrderId);
        Assert.Equal(25.00m, content.Amount);
    }

    [Fact(DisplayName = "POST /payments/refund returns ProblemDetails when amount exceeds order")]
    public async Task RefundReturnsProblemDetailsWhenAmountExceedsOrder()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        CreateOrderRequest createPayload = new("Mary Jackson", 50.00m, "USD");
        HttpResponseMessage createResponse = await client.PostAsJsonAsync("/orders", createPayload);
        CreateOrderResponse? createdOrder = await createResponse.Content.ReadFromJsonAsync<CreateOrderResponse>();

        Assert.NotNull(createdOrder);
        RefundPaymentRequest refundPayload = new(createdOrder.Id, 75.00m, "Excessive refund request");
        HttpResponseMessage response = await client.PostAsJsonAsync("/payments/refund", refundPayload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonDocument problem = await ReadJsonAsync(response);
        Assert.Equal("VALIDATION_REFUND_AMOUNT_EXCEEDS_ORDER", problem.RootElement.GetProperty("title").GetString());
        Assert.Equal("VALIDATION_REFUND_AMOUNT_EXCEEDS_ORDER", problem.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact(DisplayName = "POST /payments/refund returns ProblemDetails when order is missing")]
    public async Task RefundReturnsProblemDetailsWhenOrderIsMissing()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        RefundPaymentRequest refundPayload = new(Guid.NewGuid(), 10.00m, "Partial reimbursement");
        HttpResponseMessage response = await client.PostAsJsonAsync("/payments/refund", refundPayload);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        JsonDocument problem = await ReadJsonAsync(response);
        Assert.Equal("ORDER_NOT_FOUND", problem.RootElement.GetProperty("title").GetString());
        Assert.Equal("Cannot refund an order that does not exist.", problem.RootElement.GetProperty("detail").GetString());
    }

    [Fact(DisplayName = "POST /payments/refund returns ProblemDetails when order id is empty")]
    public async Task RefundReturnsProblemDetailsWhenOrderIdIsEmpty()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        RefundPaymentRequest refundPayload = new(Guid.Empty, 10.00m, "Partial reimbursement");
        HttpResponseMessage response = await client.PostAsJsonAsync("/payments/refund", refundPayload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonDocument problem = await ReadJsonAsync(response);
        Assert.Equal("VALIDATION_ORDER_ID", problem.RootElement.GetProperty("title").GetString());
        Assert.Equal("OrderId is required.", problem.RootElement.GetProperty("detail").GetString());
    }

    [Fact(DisplayName = "POST /payments/refund returns ProblemDetails when reason is missing")]
    public async Task RefundReturnsProblemDetailsWhenReasonIsMissing()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        RefundPaymentRequest refundPayload = new(Guid.NewGuid(), 10.00m, string.Empty);
        HttpResponseMessage response = await client.PostAsJsonAsync("/payments/refund", refundPayload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonDocument problem = await ReadJsonAsync(response);
        Assert.Equal("VALIDATION_REASON", problem.RootElement.GetProperty("title").GetString());
        Assert.Equal("Refund reason is required.", problem.RootElement.GetProperty("detail").GetString());
    }

    [Fact(DisplayName = "POST /payments/refund returns ProblemDetails when amount is not positive")]
    public async Task RefundReturnsProblemDetailsWhenAmountIsNotPositive()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        RefundPaymentRequest refundPayload = new(Guid.NewGuid(), 0, "Partial reimbursement");
        HttpResponseMessage response = await client.PostAsJsonAsync("/payments/refund", refundPayload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonDocument problem = await ReadJsonAsync(response);
        Assert.Equal("VALIDATION_REFUND_AMOUNT", problem.RootElement.GetProperty("title").GetString());
        Assert.Equal("Refund amount must be greater than zero.", problem.RootElement.GetProperty("detail").GetString());
    }

    [Fact(DisplayName = "POST /payments/refund/v2 returns OK for valid refund")]
    public async Task RefundV2ReturnsOkForValidPayload()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        CreateOrderRequest createPayload = new("Dorothy Vaughan", 99.00m, "USD");
        HttpResponseMessage createResponse = await client.PostAsJsonAsync("/orders", createPayload);
        CreateOrderResponse? createdOrder = await createResponse.Content.ReadFromJsonAsync<CreateOrderResponse>();

        Assert.NotNull(createdOrder);
        RefundPaymentRequest refundPayload = new(createdOrder.Id, 25.00m, "Partial reimbursement");
        HttpResponseMessage response = await client.PostAsJsonAsync("/payments/refund/v2", refundPayload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        RefundPaymentResponse? content = await response.Content.ReadFromJsonAsync<RefundPaymentResponse>();
        Assert.NotNull(content);
        Assert.Equal(createdOrder.Id, content.OrderId);
        Assert.Equal(25.00m, content.Amount);
    }

    [Fact(DisplayName = "POST /payments/refund/v2 returns ProblemDetails when amount exceeds order")]
    public async Task RefundV2ReturnsProblemDetailsWhenAmountExceedsOrder()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        CreateOrderRequest createPayload = new("Mary Jackson", 50.00m, "USD");
        HttpResponseMessage createResponse = await client.PostAsJsonAsync("/orders", createPayload);
        CreateOrderResponse? createdOrder = await createResponse.Content.ReadFromJsonAsync<CreateOrderResponse>();

        Assert.NotNull(createdOrder);
        RefundPaymentRequest refundPayload = new(createdOrder.Id, 75.00m, "Excessive refund request");
        HttpResponseMessage response = await client.PostAsJsonAsync("/payments/refund/v2", refundPayload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonDocument problem = await ReadJsonAsync(response);
        Assert.Equal("VALIDATION_REFUND_AMOUNT_EXCEEDS_ORDER", problem.RootElement.GetProperty("title").GetString());
        Assert.Equal("VALIDATION_REFUND_AMOUNT_EXCEEDS_ORDER", problem.RootElement.GetProperty("errorCode").GetString());
    }

    [Fact(DisplayName = "POST /payments/refund/v2 returns ProblemDetails when order is missing")]
    public async Task RefundV2ReturnsProblemDetailsWhenOrderIsMissing()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        RefundPaymentRequest refundPayload = new(Guid.NewGuid(), 10.00m, "Partial reimbursement");
        HttpResponseMessage response = await client.PostAsJsonAsync("/payments/refund/v2", refundPayload);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        JsonDocument problem = await ReadJsonAsync(response);
        Assert.Equal("ORDER_NOT_FOUND", problem.RootElement.GetProperty("title").GetString());
        Assert.Equal("Cannot refund an order that does not exist.", problem.RootElement.GetProperty("detail").GetString());
    }

    [Fact(DisplayName = "POST /payments/refund/v2 returns ProblemDetails when order id is empty")]
    public async Task RefundV2ReturnsProblemDetailsWhenOrderIdIsEmpty()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        RefundPaymentRequest refundPayload = new(Guid.Empty, 10.00m, "Partial reimbursement");
        HttpResponseMessage response = await client.PostAsJsonAsync("/payments/refund/v2", refundPayload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonDocument problem = await ReadJsonAsync(response);
        Assert.Equal("VALIDATION_ORDER_ID", problem.RootElement.GetProperty("title").GetString());
        Assert.Equal("OrderId is required.", problem.RootElement.GetProperty("detail").GetString());
    }

    [Fact(DisplayName = "POST /payments/refund/v2 returns ProblemDetails when reason is missing")]
    public async Task RefundV2ReturnsProblemDetailsWhenReasonIsMissing()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        RefundPaymentRequest refundPayload = new(Guid.NewGuid(), 10.00m, string.Empty);
        HttpResponseMessage response = await client.PostAsJsonAsync("/payments/refund/v2", refundPayload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonDocument problem = await ReadJsonAsync(response);
        Assert.Equal("VALIDATION_REASON", problem.RootElement.GetProperty("title").GetString());
        Assert.Equal("Refund reason is required.", problem.RootElement.GetProperty("detail").GetString());
    }

    [Fact(DisplayName = "POST /payments/refund/v2 returns ProblemDetails when amount is not positive")]
    public async Task RefundV2ReturnsProblemDetailsWhenAmountIsNotPositive()
    {
        await using WebApplicationFactory<Program> factory = CreateFactory();
        HttpClient client = factory.CreateClient();

        RefundPaymentRequest refundPayload = new(Guid.NewGuid(), 0, "Partial reimbursement");
        HttpResponseMessage response = await client.PostAsJsonAsync("/payments/refund/v2", refundPayload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        JsonDocument problem = await ReadJsonAsync(response);
        Assert.Equal("VALIDATION_REFUND_AMOUNT", problem.RootElement.GetProperty("title").GetString());
        Assert.Equal("Refund amount must be greater than zero.", problem.RootElement.GetProperty("detail").GetString());
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        string databaseName = $"Either.SampleApi.Tests.{Guid.NewGuid():N}";

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => _ = builder.ConfigureServices(services =>
                {
                    _ = services.RemoveAll<DbContextOptions<AppDbContext>>();
                    _ = services.AddDbContext<AppDbContext>(options =>
                        options.UseInMemoryDatabase(databaseName));
                }));
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        string content = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(content);
    }
}
