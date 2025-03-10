using Microsoft.EntityFrameworkCore;
using RapidPay.Authorization.Domain.Entities;
using RapidPay.Authorization.Infrastructure.Persistence;

namespace RapidPay.Authorization.Infrastructure.Repositories;

public class AuthLogRepository(AuthorizationDbContext context) : IAuthLogRepository
{
    public async Task AddAsync(AuthorizationLog record)
    {
        await context.AuthorizationLogs.AddAsync(record);
        await context.SaveChangesAsync();
    }

    public async Task<AuthorizationLog?> GetLastAuthorizationAsync(string cardNumber)
    {
        return await context.AuthorizationLogs
            .Where(x => x.CardNumber == cardNumber)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();
    }
}