using RapidPay.Authorization.Domain.Entities;

namespace RapidPay.Authorization.Infrastructure.Repositories;

public interface ICardAuthRepository
{
    Task<CardAuthorization?> GetByNumberAsync(string cardNumber);
    Task AddAsync(CardAuthorization card);
    Task UpdateAsync(CardAuthorization card);
}