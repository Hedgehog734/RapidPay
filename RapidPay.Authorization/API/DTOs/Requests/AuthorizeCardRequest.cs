using System.ComponentModel.DataAnnotations;

namespace RapidPay.Authorization.API.DTOs.Requests;

public record AuthorizeCardRequest
{
    [Required]
    [RegularExpression(@"^\d{15}$")]
    [Length(15, 15)]
    public string CardNumber { get; init; } = null!;
}