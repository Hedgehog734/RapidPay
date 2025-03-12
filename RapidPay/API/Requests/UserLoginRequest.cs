using System.ComponentModel.DataAnnotations;

namespace RapidPay.ApiGateway.API.Requests;

public class UserLoginRequest
{
    [Required]
    public string CardNumber { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}