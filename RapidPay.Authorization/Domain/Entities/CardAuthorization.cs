using System.ComponentModel.DataAnnotations;

namespace RapidPay.Authorization.Domain.Entities;

public class CardAuthorization
{
    [StringLength(15, MinimumLength = 15)]
    public string CardNumber { get; set; } = null!;
    public bool IsActive { get; set; }
}