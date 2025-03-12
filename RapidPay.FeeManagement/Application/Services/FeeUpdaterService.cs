using MassTransit;
using RapidPay.FeeManagement.Domain.Entities;
using RapidPay.FeeManagement.Infrastructure.Repositories;
using RapidPay.Shared.Contracts.Messaging.Events;

namespace RapidPay.FeeManagement.Application.Services;

public class FeeUpdaterService(
    IServiceScopeFactory scopeFactory,
    ILogger<FeeUpdaterService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IFeeRepository>();
                var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                var fee = await GenerateNewFeeAsync(repository);
                await repository.AddAsync(new Fee { Value = fee });

                var feeEvt = new FeeUpdatedEvent { Value = fee };
                await publisher.Publish(feeEvt, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fee updater service failed");
            }
            finally
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }

    private static async Task<decimal> GenerateNewFeeAsync(IFeeRepository repository)
    {
        var random = new Random();
        var lastFee = await repository.GetLastAsync();
        var randomMultiplier = (decimal)(random.NextDouble() * 2);

        return lastFee?.Value * randomMultiplier ?? randomMultiplier;
    }
}