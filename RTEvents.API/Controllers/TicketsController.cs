using Microsoft.AspNetCore.Mvc;

namespace RTEvents.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly ITicketsService _ticketsService;

    public TicketsController(ITicketsService ticketsService)
    {
        _ticketsService = ticketsService;
    }

    [HttpPut("{eventId}/purchase")]
    public async Task<ActionResult<dynamic>> PurchaseEventTicketAsync(
        [FromBody] PurchaseTicketRequestDTO dto,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
    {
        return Ok();
    }

    [HttpGet("{eventId}/availability")]
    public async Task<ActionResult<dynamic>> GetEventTicketAvailabilityAsync([FromRoute] int eventId)
    {
        return Ok();
    }
}