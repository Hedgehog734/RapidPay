using System.ComponentModel.DataAnnotations;

namespace RapidPay.Authorization.Domain.Entities;

public class AuthorizationLog
{
    public Guid Id { get; set; }

    [StringLength(15, MinimumLength = 15)]
    public string CardNumber { get; set; } = null!;

    public bool IsAuthorized { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Reason { get; set; } = null!;
}