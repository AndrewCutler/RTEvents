using Microsoft.AspNetCore.Mvc;
using RTEvents.API.DTOs;

namespace RTEvents.API.Controllers;

[ApiController]
[Route("[controller]")]
public class EventsController : ControllerBase
{
    private readonly IEventsService _eventsService;

    public EventsController(IEventsService eventsService)
    {
        _eventsService = eventsService;
    }

    [HttpGet("{id}", Name = nameof(GetEventByIdAsync))]
    public async Task<ActionResult<IEnumerable<WeatherForecast>>> GetEventByIdAsync([FromRoute] int id)
    {
        var result = await _eventsService.GetByIdAsync(id);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(EventDTO.FromDomain(result));
    }

    [HttpPost(Name = nameof(CreateEventAsync))]
    public async Task<ActionResult<dynamic>> CreateEventAsync([FromBody] CreateEventRequestDTO dto)
    {
        var result = await _eventsService.CreateAsync(
            name: dto.Name,
            description: dto.Description,
            date: dto.Date,
            time: dto.Time,
            timezone: dto.Timezone,
            ticketCapacity: dto.TicketCapacity
        );

        return CreatedAtRoute(
            nameof(GetEventByIdAsync),
            new { id = result.Id },
            EventDTO.FromDomain(result));
    }

    [HttpPatch(Name = nameof(UpdateEventAsync))]
    public async Task<ActionResult<EventDTO>> UpdateEventAsync([FromBody] dynamic dto)
    {
        return Ok();
    }

    [HttpDelete("{id}", Name = nameof(DeleteEventAsync))]
    public async Task<ActionResult> DeleteEventAsync([FromRoute] int id)
    {
        await _eventsService.DeleteAsync(id);

        return Ok();
    }
}
