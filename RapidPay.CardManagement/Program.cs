using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RapidPay.CardManagement.Application.EventHandlers;
using RapidPay.CardManagement.Infrastructure.Persistence;
using RapidPay.CardManagement.Infrastructure.Repositories;
using RapidPay.Shared.Configuration;
using RapidPay.Shared.Contracts.Caching;
using RapidPay.Shared.Contracts.Messaging.Events;
using RapidPay.Shared.Infrastructure.Caching;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CardDbContext>(options =>
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

        config.ReceiveEndpoint("card-updated-management", e =>
        {
            e.ConfigureConsumer<CardUpdatedEventHandler>(context);
        });

        config.ReceiveEndpoint(nameof(DepositFundsEvent), e =>
        {
            e.ConfigureConsumer<DepositFundsEventHandler>(context);
        });

        config.ReceiveEndpoint(nameof(WithdrawFundsEvent), e =>
        {
            e.ConfigureConsumer<WithdrawFundsEventHandler>(context);
        });

        config.ReceiveEndpoint(nameof(RefundRequestedEvent), e =>
        {
            e.ConfigureConsumer<RefundRequestedEventHandler>(context);
        });

        config.ConfigureEndpoints(context);
    });
});


builder.Services.AddScoped<ICardRepository, CardRepository>();
builder.Services.AddScoped<ICardTransactionRepository, CardTransactionRepository>();
builder.Services.AddScoped<ICacheService, RedisCacheService>();

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
