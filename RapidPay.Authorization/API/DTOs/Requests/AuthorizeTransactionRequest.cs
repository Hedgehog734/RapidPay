using System.ComponentModel.DataAnnotations;

namespace RapidPay.Authorization.API.DTOs.Requests;

public record AuthorizeTransactionRequest
{
    [Required]
    [RegularExpression(@"^\d{15}$")]
    [Length(15, 15)]
    public string SenderCardNumber { get; init; } = null!;

    [Required]
    [RegularExpression(@"^\d{15}$")]
    [Length(15, 15)]
    public string ReceiverCardNumber { get; init; } = null!;

    [Required]
    [Range(1, double.MaxValue, ErrorMessage = "Amount must be greater than 1")]
    public decimal Amount { get; init; }
}