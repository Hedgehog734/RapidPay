using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RapidPay.CardManagement.API.DTOs.Requests;
using RapidPay.CardManagement.Infrastructure.Commands;
using RapidPay.Shared.Contracts;

namespace RapidPay.CardManagement.API.Controllers;

[Authorize]
[Route("api/v1/cards")]
[ApiController]
public class CardsController(IMediator mediator) : ControllerBase
{
    private string? GetUserIdFromToken() =>
        HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [Authorize(Roles = "Admin")]
    [HttpPost("create")]
    public async Task<IActionResult> CreateCard([FromBody] CreateCardRequest dto)
    {
        var command = new CreateCardCommand(dto.CardNumber, dto.InitialBalance, dto.CreditLimit);
        var response = await mediator.Send(command);
        return CreatedAtAction(nameof(GetCard), new { cardNumber = response.CardNumber }, response);
    }

    [Authorize]
    [HttpGet("{cardNumber}")]
    public async Task<IActionResult> GetCard(string cardNumber)
    {
        var query = new GetCardQuery(cardNumber);
        var response = await mediator.Send(query);

        if (response == null)
        {
            return NotFound("Card not found");
        }

        if (User.IsInRole("Admin"))
        {
            return Ok(response);
        }

        var userId = GetUserIdFromToken();

        if (userId == null || response.CardNumber != userId)
        {
            return Forbid();
        }

        return Ok(response);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("update")]
    public async Task<IActionResult> UpdateCard([FromBody] UpdateCardRequest dto)
    {
        var command = new UpdateCardCommand(dto.CardNumber, dto.Balance, dto.CreditLimit);
        var result = await mediator.Send(command);
        return result ? Ok("Card updated successfully") : NotFound("Card not found");
    }
}