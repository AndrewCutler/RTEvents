using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
[ApiExplorerSettings(IgnoreApi = true)]
public class ErrorsController : ControllerBase
{
    private readonly ILogger<ErrorsController> _logger;

    public ErrorsController(ILogger<ErrorsController> logger)
    {
        _logger = logger;
    }

    public IActionResult HandleError()
    {
        var exception = HttpContext.Features.Get<IExceptionHandlerPathFeature>()?.Error;

        _logger.LogError(exception?.Message);

        return exception switch
        {
            EventNotFoundException or VenueNotFoundException or PurchaseNotFoundException => NotFound(),
            EventOverCapacityException or MissingIdempotencyKeyException or MismatchedIdempotencyKeyException => BadRequest(new { message = exception.Message }),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError, title: "An unexpeted error occurred."),
        };
    }
}