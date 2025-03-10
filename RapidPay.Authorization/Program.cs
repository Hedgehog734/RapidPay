using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RapidPay.Authorization.Application.EventHandlers;
using RapidPay.Authorization.Application.Services;
using RapidPay.Authorization.Infrastructure.Persistence;
using RapidPay.Authorization.Infrastructure.Repositories;
using RapidPay.Shared.Configuration;
using RapidPay.Shared.Contracts;
using RapidPay.Shared.Contracts.Caching;
using RapidPay.Shared.Infrastructure.Caching;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AuthorizationDbContext>(options =>
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
    x.AddRequestClient<GetCardQuery>();

    x.UsingRabbitMq((context, config) =>
    {
        var rabbitSettings = context.GetRequiredService<IOptions<RabbitMqSettings>>().Value;

        config.Host(rabbitSettings.HostName, h =>
        {
            h.Username(rabbitSettings.UserName);
            h.Password(rabbitSettings.Password);
        });

        config.ReceiveEndpoint("card-updated-auth", e =>
        {
            e.ConfigureConsumer<CardUpdatedEventHandler>(context);
        });

        config.ConfigureEndpoints(context);
    });
});

builder.Services.AddScoped<ICardAuthRepository, CardAuthRepository>();
builder.Services.AddScoped<IAuthLogRepository, AuthLogRepository>();
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<ICardManagementService, CardManagementService>();

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
