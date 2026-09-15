using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
[ApiExplorerSettings(IgnoreApi = true)]
public class ErrorsController : ControllerBase
{
    public IActionResult HandleError()
    {
        return Problem();
    }
}