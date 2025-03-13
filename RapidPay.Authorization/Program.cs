using System.Security.Claims;
using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RapidPay.Authorization.Application.EventHandlers;
using RapidPay.Authorization.Application.Services;
using RapidPay.Authorization.Infrastructure.Configuration;
using RapidPay.Authorization.Infrastructure.Persistence;
using RapidPay.Authorization.Infrastructure.Repositories;
using RapidPay.Shared.Configuration;
using RapidPay.Shared.Contracts;
using RapidPay.Shared.Contracts.Caching;
using RapidPay.Shared.Infrastructure.Caching;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AuthorizationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));
builder.Services.Configure<ServiceRedisSettings>(builder.Configuration.GetSection("Redis"));
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

builder.Services.AddScoped<IDatabase>(config =>
{
    var rabbitSettings = config.GetRequiredService<IOptions<RabbitMqSettings>>().Value;
    var multiplexer = ConnectionMultiplexer.Connect($"{rabbitSettings.HostName},password={rabbitSettings.Password}");
    return multiplexer.GetDatabase();
});


builder.Services.AddMediatR(x => x.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(x =>
{
    x.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "{your_token}",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });

    x.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            []
        }
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            RoleClaimType = ClaimTypes.Role
        };
    });

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
