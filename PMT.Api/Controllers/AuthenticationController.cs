using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using PMT.Data.Entities;
using PMT.Services;
using System.Net;

namespace PMT.Api.Controllers;

[ApiController]
public class AuthenticationController(AuthenticationService _authService) : ControllerBase {

    [HttpPost("[action]")]
    public async Task<IActionResult> Login([FromBody] string? GoogleIdToken) {
        Console.WriteLine($"AuthController.Login(string? GoogleIdToken)");

        LoginResult result = await _authService.LoginAsync(GoogleIdToken, Request.HttpContext.Connection.RemoteIpAddress);

        if (!result.Succeeded) {
            if (result.HttpStatusCode == HttpStatusCode.BadRequest) {
                Console.WriteLine($"AuthController.Login() - BadRequest({result.Message})");
                return BadRequest(result.Message);
            } else if (result.HttpStatusCode == HttpStatusCode.Forbidden) {
                Console.WriteLine($"AuthController.Login() - Forbidden({result.Message})");
                return Forbid();
            }
            
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
        
        if (result.AccessToken is null || result.RefreshToken is null)
            return StatusCode(StatusCodes.Status500InternalServerError);

        _authService.CreateRefreshCookie(Response.Cookies, result.RefreshToken);
        return Ok(new { access_token = result.AccessToken });
    }

    [Authorize]
    [HttpPost("[action]")]
    public async Task<IActionResult> Logout() {
        Console.WriteLine("AuthenticationController.Logout()");
        if (!Request.Cookies.TryGetValue("refresh_token", out string? refresh_cookie)) {
            Console.WriteLine("AuthController.Refresh() - Forbid(NoRefreshToken)");
            return Forbid();
        }

        LogoutResult result = await _authService.LogoutAsync(refresh_cookie);

        if (!result.Succeeded) {
            if (result.HttpStatusCode == HttpStatusCode.Forbidden) {
                Console.WriteLine($"AuthController.Logout() - Forbidden({result.Message})");
                return Forbid();
            }
            
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
        
        return Ok();
    }

    [Authorize]
    [HttpPost("[action]")]
    public async Task<IActionResult> FullLogout() {
        Console.WriteLine("AuthenticationController.LogoutAll()");
        if (!Request.Cookies.TryGetValue("refresh_token", out string? refresh_cookie)) {
            Console.WriteLine("AuthController.Refresh() - Forbid(NoRefreshToken)");
            return Forbid();
        }

        LogoutResult result = await _authService.FullLogoutAsync(refresh_cookie);
        
        if (!result.Succeeded) {
            if (result.HttpStatusCode == HttpStatusCode.Forbidden) {
                Console.WriteLine($"AuthController.Logout() - Forbidden({result.Message})");
                return Forbid();
            }
            
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        return Ok();
    }

    [HttpPost("access")]
    public async Task<IActionResult> NewAccessToken() {
        Console.WriteLine("AuthController.Access()");

        if (!Request.Cookies.TryGetValue("refresh_token", out string? refresh_cookie)) {
            Console.WriteLine("AuthController.Access() - Forbidden(NoRefreshCookie)");
            return Forbid();
        }

        AccessResult result = await _authService.CreateAccessTokenAsync(refresh_cookie);

        if (!result.Succeeded) {
            if (result.HttpStatusCode == HttpStatusCode.Forbidden) {
                Console.WriteLine($"AuthController.Access() - Forbidden({result.Message})");
                return Forbid();
            }

            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        return Ok(new { access_token = result.AccessToken });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> NewRefreshToken() {
        Console.WriteLine("AuthController.Refresh()");

        IPAddress? ipAddress = Request.HttpContext.Connection.RemoteIpAddress;
        if (ipAddress is null) {
            Console.WriteLine("AuthController.Refresh() - BadRequest(BadIpAddress)");
            return BadRequest();
        }

        if (!Request.Cookies.TryGetValue("refresh_token", out string? refresh_cookie)) {
            Console.WriteLine("AuthController.Refresh() - Forbidden(NoRefreshCookie)");
            return Forbid();
        }

        RefreshResult result = await _authService.CreateRefreshTokenAsync(refresh_cookie, ipAddress);
        
        if (!result.Succeeded) {
            if (result.HttpStatusCode == HttpStatusCode.Forbidden) {
                Console.WriteLine($"AuthController.Refresh() - Forbidden({result.Message})");
                return Forbid();
            }

            return StatusCode(StatusCodes.Status500InternalServerError);
        }
        

        _authService.CreateRefreshCookie(Response.Cookies, result.RefreshToken!);

        return Ok();
    }
}