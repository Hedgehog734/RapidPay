using MediatR;
using Microsoft.Extensions.Options;
using RapidPay.CardManagement.Infrastructure.Repositories;
using RapidPay.Shared.Configuration;
using RapidPay.Shared.Contracts;
using RapidPay.Shared.Contracts.Caching;
using RapidPay.Shared.Infrastructure.Caching;

namespace RapidPay.CardManagement.Infrastructure.Queries.Handlers;

public class GetCardHandler(
    ICardRepository cardRepository,
    ICacheService cacheService,
    IOptions<RedisSettings> settings)
    : IRequestHandler<GetCardQuery, CardResponseDto?>
{
    private readonly RedisSettings _settings = settings.Value;

    public async Task<CardResponseDto?> Handle(GetCardQuery request, CancellationToken cancellationToken)
    {
        var cardKey = CacheKeys.CardData(request.CardNumber);
        var cachedCard = await cacheService.GetAsync<CachedCardData>(cardKey);

        if (cachedCard != null)
        {
            return new CardResponseDto(cachedCard.CardNumber, cachedCard.Balance, cachedCard.CreditLimit,
                cachedCard.UsedCredit);
        }

        var card = await cardRepository.GetByNumberAsync(request.CardNumber);

        if (card == null)
        {
            return null;
        }

        cachedCard = new CachedCardData(card.CardNumber, card.Balance, card.CreditLimit, card.UsedCredit);
        var expiration = TimeSpan.FromSeconds(_settings.CacheDurationSeconds);
        await cacheService.SetAsync(cardKey, cachedCard, expiration);

        var cardDto = new CardResponseDto(card.CardNumber, card.Balance, card.CreditLimit, card.UsedCredit);
        return cardDto;
    }
}