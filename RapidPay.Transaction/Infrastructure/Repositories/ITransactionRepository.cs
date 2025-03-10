using RapidPay.Transaction.Domain.Entities;

namespace RapidPay.Transaction.Infrastructure.Repositories;

public interface ITransactionRepository
{
    public Task AddTransactionAsync(CardTransaction transaction);
    public Task<CardTransaction?> GetByIdAsync(Guid id);
    Task UpdateStatusAsync(Guid transactionId, string status);
}