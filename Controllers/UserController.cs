using System;
using Microsoft.AspNetCore.Mvc;
using Tutag.Models;
using Tutag.Services;

namespace Tutag.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsersController : ControllerBase
    {
        private IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("authenticate")]
        public IActionResult Authenticate([FromForm] AuthenticateRequest model, [FromForm] string redirect)
        {
            var token = _userService.Authenticate(model);

            if (token == null)
                return BadRequest(new { message = "Couldn't validate request" });

            // TODO: better way to handle this?
            // maybe use the Authorization header without setting a cookie
            Response.Cookies.Append("Authorization", token, new Microsoft.AspNetCore.Http.CookieOptions
            {
                MaxAge = TimeSpan.FromDays(1),
            });

            if (redirect.ToLower().EndsWith("login"))
            {
                return Redirect("/");
            }
            else
            {
                return Redirect(redirect);
            }
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("Authorization");

            return Redirect("/");
        }
    }
}