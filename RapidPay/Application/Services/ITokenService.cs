namespace RapidPay.ApiGateway.Application.Services;

public interface ITokenService
{
    string GenerateJwtToken(string username, string[] roles);
}