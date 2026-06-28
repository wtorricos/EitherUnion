using Either.SampleApi.Features.Orders.Create;
using Either.SampleApi.Features.Orders.GetById;
using Either.SampleApi.Features.Payments.Refund;
using Either.SampleApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase("Either.SampleApi"));

WebApplication app = builder.Build();
app.MapCreateOrderEndpoints();
app.MapGetOrderByIdEndpoints();
app.MapRefundPaymentEndpoints();
app.Run();
