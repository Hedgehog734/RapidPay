namespace RapidPay.CardManagement.API.DTOs.Requests;

public record UpdateCardRequest : BaseCardRequest
{
    public decimal? Balance { get; init; }
    public decimal? CreditLimit { get; init; }
}