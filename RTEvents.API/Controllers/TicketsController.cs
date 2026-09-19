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

    [HttpPut("purchase")]
    public async Task<ActionResult<PurchaseTicketsResponseDTO>> PurchaseEventTicketAsync(
        [FromBody] PurchaseTicketRequestDTO dto,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var response = await _ticketsService.PurchaseTicketsAsync(
            quantity: dto.Quantity,
            eventId: dto.EventId,
            paymentDetails: dto.PaymentDetails,
            idempotencyKey: idempotencyKey,
            cancellationToken: cancellationToken);

        return Ok(PurchaseTicketsResponseDTO.FromDomain(response));
    }

    [HttpGet("availability")]
    public async Task<ActionResult<TicketAvailabilityDTO>> GetEventTicketAvailabilityAsync([FromQuery] int eventId, CancellationToken cancellationToken = default)
    {
        var result = await _ticketsService.GetTicketAvailabilityAsync(eventId, cancellationToken);

        return Ok(TicketAvailabilityDTO.FromDomain(result));
    }
}