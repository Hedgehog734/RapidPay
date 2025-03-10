using RapidPay.CardManagement.Domain.Entities;

namespace RapidPay.CardManagement.Infrastructure.Repositories;

public interface ICardTransactionRepository
{
    Task AddAsync(CardTransactionLog record);
}