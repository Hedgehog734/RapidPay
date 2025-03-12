using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RapidPay.Shared.Configuration;
using RapidPay.Shared.Contracts.Caching;
using RapidPay.Shared.Contracts.Messaging.Events;
using RapidPay.Shared.Infrastructure.Caching;
using RapidPay.Transaction.Application.EventHandlers;
using RapidPay.Transaction.Infrastructure.Persistent;
using RapidPay.Transaction.Infrastructure.Repositories;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<TransactionDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration["Redis:ConnectionString"];
});

builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMQ"));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumers(typeof(Program).Assembly);

    x.UsingRabbitMq((context, config) =>
    {
        var rabbitSettings = context.GetRequiredService<IOptions<RabbitMqSettings>>().Value;

        config.Host(rabbitSettings.HostName, h =>
        {
            h.Username(rabbitSettings.UserName);
            h.Password(rabbitSettings.Password);
        });

        config.ReceiveEndpoint(nameof(FundsWithdrawnEvent), e =>
        {
            e.ConfigureConsumer<FundsWithdrawnEventHandler>(context);
        });

        config.ReceiveEndpoint(nameof(TransactionAuthorizedEvent), e =>
        {
            e.ConfigureConsumer<TransactionAuthorizedEventHandler>(context);
        });

        config.ReceiveEndpoint(nameof(TransactionCompletedEvent), e =>
        {
            e.ConfigureConsumer<TransactionCompletedEventHandler>(context);
        });

        config.ReceiveEndpoint(nameof(TransactionFailedEvent), e =>
        {
            e.ConfigureConsumer<TransactionFailedEventHandler>(context);
        });

        config.ReceiveEndpoint(nameof(TransactionRefundedEvent), e =>
        {
            e.ConfigureConsumer<TransactionRefundedEventHandler>(context);
        });

        config.ConfigureEndpoints(context);
    });
});

builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ICacheService, RedisCacheService>();

builder.Services.AddScoped<IDatabase>(config =>
{
    var rabbitSettings = config.GetRequiredService<IOptions<RabbitMqSettings>>().Value;
    var multiplexer = ConnectionMultiplexer.Connect($"{rabbitSettings.HostName},password={rabbitSettings.Password}");
    return multiplexer.GetDatabase();
});

builder.Services.AddMediatR(x => x.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
