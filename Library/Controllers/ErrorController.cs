using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Library.Controllers
{
    [AllowAnonymous]
    public class ErrorController : Controller
    {
        [Route("Error/{statusCode:int}")]
        public IActionResult Handle(int statusCode)
        {
            Response.StatusCode = statusCode;

            return statusCode switch
            {
                StatusCodes.Status404NotFound =>
                    View("NotFound404"),

                StatusCodes.Status500InternalServerError =>
                    View("ServerError500"),

                _ => View("ServerError500")
            };
        }
    }
}