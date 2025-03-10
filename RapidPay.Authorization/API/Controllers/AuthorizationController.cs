using MediatR;
using Microsoft.AspNetCore.Mvc;
using RapidPay.Authorization.API.DTOs.Requests;
using RapidPay.Authorization.Infrastructure.Commands;

namespace RapidPay.Authorization.API.Controllers;

[Route("api/v1/authorization")]
[ApiController]
public class CardsController(IMediator mediator) : ControllerBase
{
    [HttpPost("transaction")]
    public async Task<IActionResult> AuthorizeTransaction([FromBody] AuthorizeTransactionRequest request)
    {
        var command = new AuthorizeTransactionCommand(request.SenderCardNumber, request.ReceiverCardNumber, request.Amount);
        var isAuthorized = await mediator.Send(command);
        return isAuthorized ? Ok(new { Message = "Transaction authorized" }) : BadRequest(new { Message = "Authorization failed" });
    }

    [HttpPost("card")]
    public async Task<IActionResult> AuthorizeCard([FromBody] AuthorizeCardRequest request)
    {
        var command = new AuthorizeCardCommand(request.CardNumber);
        var isAuthorized = await mediator.Send(command);
        return isAuthorized ? Ok(new { Message = "Card authorized" }) : BadRequest(new { Message = "Authorization failed" });
    }
}