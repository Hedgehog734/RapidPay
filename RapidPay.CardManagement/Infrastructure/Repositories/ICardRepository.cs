using RapidPay.CardManagement.Domain.Entities;

namespace RapidPay.CardManagement.Infrastructure.Repositories;

public interface ICardRepository
{
    Task<Card?> GetByNumberAsync(string cardNumber);
    Task<Card> AddAsync(Card card);
    Task UpdateAsync(Card card);
    Task<bool> WithdrawAsync(string cardNumber, decimal amount);
    Task<bool> DepositAsync(string cardNumber, decimal amount);
}