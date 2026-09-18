using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
[ApiExplorerSettings(IgnoreApi = true)]
public class ErrorsController : ControllerBase
{
    public IActionResult HandleError()
    {
        var exception = HttpContext.Features.Get<IExceptionHandlerPathFeature>()?.Error;

        return exception switch
        {
            EventNotFoundException or VenueNotFoundException => NotFound(),
            EventOverCapacityException => BadRequest(new { message = exception.Message }),
            _ => Problem(statusCode: StatusCodes.Status500InternalServerError, title: "An unexpeted error occurred."),
        };
    }
}