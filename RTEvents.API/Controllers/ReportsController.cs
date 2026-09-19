using Microsoft.AspNetCore.Mvc;

namespace RTEvents.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportingService _reportingService;

    public ReportsController(IReportingService reportingService)
    {
        _reportingService = reportingService;
    }

    [HttpGet]
    public async Task<ActionResult<EventsReportDTO>> GetEventsReportsAsync([FromQuery] int skip, [FromQuery] int take, CancellationToken cancellationToken = default)
    {
        var result = await _reportingService.GenerateReportByEventAsync(skip: skip, take: take, cancellationToken: cancellationToken);

        return Ok(EventsReportDTO.FromDomain(result));
    }
}