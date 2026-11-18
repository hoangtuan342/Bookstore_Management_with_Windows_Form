using Microsoft.AspNetCore.Mvc;

namespace TrangSach.API.Controllers;

public static class ControllerExceptionHelper
{
    public static IActionResult Execute(Action action)
    {
        try
        {
            action();
            return new NoContentResult();
        }
        catch (ArgumentException ex)
        {
            return new BadRequestObjectResult(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return new NotFoundObjectResult(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return new ConflictObjectResult(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return new UnauthorizedObjectResult(new { message = ex.Message });
        }
    }

    public static IActionResult Execute<T>(Func<T> action, int successStatusCode = 200, string? createdUri = null)
    {
        try
        {
            var value = action();
            return successStatusCode switch
            {
                201 when createdUri != null => new CreatedResult(createdUri, value),
                201 => new ObjectResult(value) { StatusCode = 201 },
                _ => new OkObjectResult(value)
            };
        }
        catch (ArgumentException ex)
        {
            return new BadRequestObjectResult(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return new NotFoundObjectResult(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return new ConflictObjectResult(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return new UnauthorizedObjectResult(new { message = ex.Message });
        }
    }
}
