using MediatR;
using Microsoft.AspNetCore.Mvc;
using RapidPay.CardManagement.API.DTOs.Requests;
using RapidPay.CardManagement.Infrastructure.Commands;
using RapidPay.Shared.Contracts;

namespace RapidPay.CardManagement.API.Controllers;

[Route("api/v1/cards")]
[ApiController]
public class CardsController(IMediator mediator) : ControllerBase
{
    [HttpPost("create")]
    public async Task<IActionResult> CreateCard([FromBody] CreateCardRequest dto)
    {
        var command = new CreateCardCommand(dto.CardNumber, dto.InitialBalance, dto.CreditLimit);
        var response = await mediator.Send(command);
        return CreatedAtAction(nameof(GetCardBalance), new { cardNumber = response.CardNumber }, response);
    }

    [HttpGet("{cardNumber}")]
    public async Task<IActionResult> GetCardBalance(string cardNumber)
    {
        var query = new GetCardQuery(cardNumber);
        var response = await mediator.Send(query);
        return response != null ? Ok(response) : NotFound("Card not found");
    }

    [HttpPut("update")]
    public async Task<IActionResult> UpdateCard([FromBody] UpdateCardRequest dto)
    {
        var command = new UpdateCardCommand(dto.CardNumber, dto.Balance, dto.CreditLimit);
        var result = await mediator.Send(command);
        return result ? Ok("Card updated successfully") : NotFound("Card not found");
    }
}