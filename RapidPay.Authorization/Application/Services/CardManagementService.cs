using MassTransit;
using RapidPay.Shared.Contracts;
using RapidPay.Shared.Contracts.Messaging;

namespace RapidPay.Authorization.Application.Services;

public class CardManagementService(
    IRequestClient<GetCardQuery> requestClient,
    ILogger<CardManagementService> logger)
    : ICardManagementService
{
    public async Task<Result<CardResponseDto>?> GetCardAsync(string cardNumber)
    {
        try
        {
            var response = await requestClient.GetResponse<Result<CardResponseDto>>(new GetCardQuery(cardNumber));
            return response.Message;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"{nameof(CardManagementService)} error: {ex.Message}");
            return null;
        }
    }
}