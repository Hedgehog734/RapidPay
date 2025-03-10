using RapidPay.Authorization.Domain.Entities;

namespace RapidPay.Authorization.Infrastructure.Repositories;

public interface IAuthLogRepository
{
    Task AddAsync(AuthorizationLog record);
    Task<AuthorizationLog?> GetLastAuthorizationAsync(string cardNumber);
}