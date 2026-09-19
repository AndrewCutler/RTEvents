using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace RTEvents.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessageBusController : ControllerBase
{
    private readonly RTEventsDbContext _context;

    public MessageBusController(RTEventsDbContext context)
    {
        _context = context;
    }

    [HttpPost(Name = nameof(CreatePaymentResponseAsync))]
    public async Task<ActionResult> CreatePaymentResponseAsync([FromBody] CreatePaymentResponseRequestDTO dto)
    {
        var response = new PaymentResponseEvent(dto.PaymentId, dto.Success);

        _context.Messages.Add(new Message
        {
            Type = dto.Success ? MessageType.PaymentSucceeded : MessageType.PaymentFailed,
            Payload = JsonSerializer.Serialize(response),
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await _context.SaveChangesAsync();

        return Ok();
    }
}
