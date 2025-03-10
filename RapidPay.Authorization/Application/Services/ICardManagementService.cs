using RapidPay.Shared.Contracts;
using RapidPay.Shared.Contracts.Messaging;

namespace RapidPay.Authorization.Application.Services;

public interface ICardManagementService
{
    Task<Result<CardResponseDto>?> GetCardAsync(string cardNumber);
}