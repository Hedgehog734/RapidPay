using RapidPay.CardManagement.Domain.Entities;
using RapidPay.CardManagement.Infrastructure.Persistence;

namespace RapidPay.CardManagement.Infrastructure.Repositories;

public class CardTransactionRepository(CardDbContext context) : ICardTransactionRepository
{
    public async Task AddAsync(CardTransactionLog record)
    {
        await context.CardsLogs.AddAsync(record);
        await context.SaveChangesAsync();
    }
}