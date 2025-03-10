using System.ComponentModel.DataAnnotations;

namespace RapidPay.CardManagement.API.DTOs.Requests;

public record BaseCardRequest
{
    [Required]
    [RegularExpression(@"^\d{15}$")]
    [Length(15, 15)]
    public string CardNumber { get; set; } = null!;
}