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
            switch (result.FailureCode) {
                case LoginFailureCode.BadToken:
                case LoginFailureCode.MissingIpAddress:
                    Console.WriteLine($"AuthController.Login() - BadRequest({nameof(result.FailureCode)})");
                    return BadRequest(result.Message);

                case LoginFailureCode.GoogleVerification:
                case LoginFailureCode.Unauthorized:
                case LoginFailureCode.UserInactive:
                    Console.WriteLine($"AuthController.Login() - Unauthorized({nameof(result.FailureCode)})");
                    return Unauthorized(result.Message);
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
            return Forbid("Missing refresh token");  // No refresh token was found
        }

        LogoutResult result = await _authService.LogoutAsync(refresh_cookie);

        if (!result.Succeeded) {
            switch (result.FailureCode) {
                case LogoutFailureCode.RefreshTokenRevoked:
                    Console.WriteLine($"AuthController.Logout() - Unauthorized({nameof(result.FailureCode)})");
                    return Unauthorized(result.Message);

                case LogoutFailureCode.RefreshTokenNotFound:
                case LogoutFailureCode.RefreshTokenExpired:
                    Console.WriteLine($"AuthController.Logout() - Forbid({nameof(result.FailureCode)})");
                    return Forbid(result.Message!);
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
            return Forbid("Missing refresh token");  // No refresh token was found
        }

        LogoutResult result = await _authService.FullLogoutAsync(refresh_cookie);
        
        if (!result.Succeeded) {
            switch (result.FailureCode) {
                case LogoutFailureCode.RefreshTokenRevoked:
                    Console.WriteLine($"AuthController.Logout() - Unauthorized({nameof(result.FailureCode)})");
                    return Unauthorized(result.Message);

                case LogoutFailureCode.RefreshTokenNotFound:
                case LogoutFailureCode.RefreshTokenExpired:
                    Console.WriteLine($"AuthController.Logout() - Forbid({nameof(result.FailureCode)})");
                    return Forbid(result.Message!);
            }
            
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
        
        return Ok();
    }

    [HttpPost("access")]
    public async Task<IActionResult> NewAccessToken() {
        Console.WriteLine("AuthController.Access()");

        if (!Request.Cookies.TryGetValue("refresh_token", out string? refresh_cookie)) {
            Console.WriteLine("AuthController.Access() - Unauthorized(MissingRefreshCookie)");
            return Unauthorized("Missing refresh cookie");
        }

        AccessResult result = await _authService.CreateAccessTokenAsync(refresh_cookie);

        if (!result.Succeeded) {
            switch (result.FailureCode) {
                case RefreshTokenFailureCode.Missing:
                case RefreshTokenFailureCode.Expired:
                case RefreshTokenFailureCode.Revoked:
                    Console.WriteLine($"AuthController.Access() - Unauthorized({nameof(result.FailureCode)})");
                    return Unauthorized(result.Message);
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
            Console.WriteLine("AuthController.Refresh() - Unauthorized(BadIpAddress)");
            return Unauthorized();
        }

        if (!Request.Cookies.TryGetValue("refresh_token", out string? refresh_cookie)) {
            Console.WriteLine("AuthController.Refresh() - Unauthorized(NoRefreshCookie)");
            return Unauthorized("No refresh cookie");  // No refresh token was found
        }

        RefreshResult result = await _authService.CreateRefreshTokenAsync(refresh_cookie, ipAddress);
        
        if (!result.Succeeded) {
            switch (result.FailureCode) {
                case RefreshTokenFailureCode.Missing:
                case RefreshTokenFailureCode.Expired:
                case RefreshTokenFailureCode.Revoked:
                    Console.WriteLine($"AuthController.Refresh() - Unauthorized({nameof(result.FailureCode)})");
                    return Unauthorized(result.Message);
            }

            return StatusCode(StatusCodes.Status500InternalServerError);
        }
        

        _authService.CreateRefreshCookie(Response.Cookies, result.RefreshToken!);

        return Ok();
    }
}