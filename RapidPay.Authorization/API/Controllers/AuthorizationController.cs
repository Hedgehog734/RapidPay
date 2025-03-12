using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RapidPay.Authorization.API.DTOs.Requests;
using RapidPay.Authorization.Infrastructure.Commands;

namespace RapidPay.Authorization.API.Controllers;

[Authorize]
[Route("api/v1/authorization")]
[ApiController]
public class CardsController(IMediator mediator) : ControllerBase
{
    [Authorize]
    [HttpPost("transaction")]
    public async Task<IActionResult> AuthorizeTransaction([FromBody] AuthorizeTransactionRequest request)
    {
        var result = ValidateUser(request.SenderCardNumber);

        if (result != null)
        {
            return result;
        }

        var command = new AuthorizeTransactionCommand(request.SenderCardNumber, request.ReceiverCardNumber, request.Amount);
        var isAuthorized = await mediator.Send(command);
        return isAuthorized ? Ok(new { Message = "Transaction authorized" }) : BadRequest(new { Message = "Authorization failed" });
    }

    [Authorize]
    [HttpPost("card")]
    public async Task<IActionResult> AuthorizeCard([FromBody] AuthorizeCardRequest request)
    {
        var result = ValidateUser(request.CardNumber);

        if (result != null)
        {
            return result;
        }

        var command = new AuthorizeCardCommand(request.CardNumber);
        var isAuthorized = await mediator.Send(command);
        return isAuthorized ? Ok(new { Message = "Card authorized" }) : BadRequest(new { Message = "Authorization failed" });
    }

    private IActionResult? ValidateUser(string cardNumber)
    {
        var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
        {
            return Unauthorized();
        }

        if (User.IsInRole("User") && cardNumber != userId)
        {
            return Forbid();
        }

        return null;
    }
}