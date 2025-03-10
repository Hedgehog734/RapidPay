using RapidPay.Transaction.Domain.Entities;
using RapidPay.Transaction.Infrastructure.Persistent;

namespace RapidPay.Transaction.Infrastructure.Repositories;

public class TransactionRepository(TransactionDbContext context) : ITransactionRepository
{
    public async Task AddTransactionAsync(CardTransaction transaction)
    {
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();
    }

    public async Task<CardTransaction?> GetByIdAsync(Guid id)
    {
        return await context.Transactions.FindAsync(id);
    }

    public async Task UpdateStatusAsync(Guid id, string status)
    {
        var transaction = await GetByIdAsync(id);

        if (transaction != null)
        {
            transaction.Status = status;
            transaction.UpdatedAt = DateTime.UtcNow;
            context.Transactions.Update(transaction);

            await context.SaveChangesAsync();
        }
    }
}