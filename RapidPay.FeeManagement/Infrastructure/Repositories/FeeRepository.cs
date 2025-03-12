using Microsoft.EntityFrameworkCore;
using RapidPay.FeeManagement.Domain.Entities;
using RapidPay.FeeManagement.Infrastructure.Persistence;

namespace RapidPay.FeeManagement.Infrastructure.Repositories;

public class FeeRepository(FeeDbContext context) : IFeeRepository
{
    public async Task<Fee?> GetLastAsync()
    {
        return await context.Fees
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<Fee> AddAsync(Fee fee)
    {
        await context.Fees.AddAsync(fee);
        await context.SaveChangesAsync();
        return fee;
    }
}