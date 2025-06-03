using ChatApp.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BaseController : ControllerBase
    {
        protected ApplicationUser? CurrentUser => HttpContext.Items["CurrentUser"] as ApplicationUser;
        protected int CurrentUserId => (int)(HttpContext.Items["CurrentUserId"] ?? 0);

        protected IActionResult HandleResult<T>(T result)
        {
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        protected IActionResult HandleResult<T>(T result, string message)
        {
            if (result == null)
                return NotFound(new { message });

            return Ok(new { data = result, message });
        }
    }
}
