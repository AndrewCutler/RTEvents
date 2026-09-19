using Microsoft.AspNetCore.Mvc;
using RTEvents.API.DTOs;

namespace RTEvents.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventsService _eventsService;

    public EventsController(IEventsService eventsService)
    {
        _eventsService = eventsService;
    }

    [HttpGet("{id}", Name = nameof(GetEventByIdAsync))]
    public async Task<ActionResult<EventDTO>> GetEventByIdAsync([FromRoute] int id, CancellationToken cancellationToken = default)
    {
        var result = await _eventsService.GetByIdAsync(id, cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(EventDTO.FromDomain(result));
    }

    [HttpPost(Name = nameof(CreateEventAsync))]
    public async Task<ActionResult<EventDTO>> CreateEventAsync([FromBody] CreateEventRequestDTO dto, CancellationToken cancellationToken = default)
    {
        var result = await _eventsService.CreateAsync(
            name: dto.Name,
            description: dto.Description,
            date: dto.Date,
            time: dto.Time,
            timezone: dto.Timezone,
            ticketCapacity: dto.TicketCapacity,
            venueId: dto.VenueId,
            cancellationToken: cancellationToken
        );

        return CreatedAtRoute(
            nameof(GetEventByIdAsync),
            new { id = result.Id },
            EventDTO.FromDomain(result));
    }

    [HttpPatch(Name = nameof(UpdateEventAsync))]
    public async Task<ActionResult<EventDTO>> UpdateEventAsync([FromBody] UpdateEventRequestDTO dto, CancellationToken cancellationToken = default)
    {
        var result = await _eventsService.UpdateAsync(
            id: dto.Id,
            name: dto.Name,
            description: dto.Description,
            date: dto.Date,
            time: dto.Time,
            timezone: dto.Timezone,
            ticketCapacity: dto.TicketCapacity,
            cancellationToken: cancellationToken);

        return Ok(EventDTO.FromDomain(result));
    }

    [HttpDelete("{id}", Name = nameof(DeleteEventAsync))]
    public async Task<ActionResult> DeleteEventAsync([FromRoute] int id, CancellationToken cancellationToken = default)
    {
        await _eventsService.DeleteAsync(id, cancellationToken);

        return Ok();
    }
}
