namespace RapidPay.CardManagement.API.DTOs.Requests;

public record CreateCardRequest : BaseCardRequest
{
    public decimal InitialBalance { get; init; }
    public decimal? CreditLimit { get; init; }
}