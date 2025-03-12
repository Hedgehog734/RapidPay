using RapidPay.FeeManagement.Domain.Entities;

namespace RapidPay.FeeManagement.Infrastructure.Repositories;

public interface IFeeRepository
{
    Task<Fee?> GetLastAsync();

    Task<Fee> AddAsync(Fee fee);
}