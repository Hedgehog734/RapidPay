using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RapidPay.ApiGateway.API.Requests;
using RapidPay.ApiGateway.Application.Services;

namespace RapidPay.ApiGateway.API.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/auth")]
public class AuthController(ITokenService tokenService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public IActionResult Login([FromBody] UserLoginRequest request) // simple authentication for now
    {
        if (request is { CardNumber: "admin", Password: "password" })
        {
            var roles = new[] { "Admin", "User" };
            return OkWithRoles(request.CardNumber, roles);
        }

        if (request is { Password: "password" })
        {
            var roles = new[] { "User" };
            return OkWithRoles(request.CardNumber, roles);
        }

        return Unauthorized();
    }

    private IActionResult OkWithRoles(string cardNumber, string[] roles)
    {
        var token = tokenService.GenerateJwtToken(cardNumber, roles);
        return Ok(new { token });
    }
}