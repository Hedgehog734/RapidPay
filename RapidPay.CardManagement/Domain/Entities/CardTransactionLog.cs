using System.ComponentModel.DataAnnotations;

namespace RapidPay.CardManagement.Domain.Entities;

public class CardTransactionLog
{
    public Guid Id { get; set; }

    [StringLength(15, MinimumLength = 15)]
    public string CardNumber { get; set; } = null!;

    public decimal Amount { get; set; } 

    public string TransactionType { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}