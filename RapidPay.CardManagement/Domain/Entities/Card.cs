using System.ComponentModel.DataAnnotations;

namespace RapidPay.CardManagement.Domain.Entities;

public class Card
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [StringLength(15, MinimumLength = 15)]
    public string CardNumber { get; set; } = null!;

    public decimal Balance { get; set; }

    public decimal? CreditLimit { get; set; }

    public decimal? UsedCredit { get; set; }
}