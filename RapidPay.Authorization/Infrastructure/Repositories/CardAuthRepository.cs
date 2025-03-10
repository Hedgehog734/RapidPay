using RapidPay.Authorization.Domain.Entities;
using RapidPay.Authorization.Infrastructure.Persistence;

namespace RapidPay.Authorization.Infrastructure.Repositories;

public class CardAuthRepository(AuthorizationDbContext dbContext) : ICardAuthRepository
{
    public async Task<CardAuthorization?> GetByNumberAsync(string cardNumber)
    {
        return await dbContext.CardAuthorizations.FindAsync(cardNumber);
    }

    public async Task AddAsync(CardAuthorization card)
    {
        await dbContext.CardAuthorizations.AddAsync(card);
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(CardAuthorization card)
    {
        dbContext.CardAuthorizations.Update(card);
        await dbContext.SaveChangesAsync();
    }
}