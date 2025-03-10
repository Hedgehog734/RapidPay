using RapidPay.Shared.Constants;

namespace RapidPay.Transaction.Domain.Entities
{
    public class CardTransaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required string SenderNumber { get; set; }
        public required string RecipientNumber { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = TransactionStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; }
    }
}
