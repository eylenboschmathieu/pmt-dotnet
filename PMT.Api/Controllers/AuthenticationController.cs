using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Google.Apis.Auth;

using PMT.Data.Entities;
using PMT.Services;
using System.Net;
using Microsoft.AspNetCore.Http.HttpResults;

namespace PMT.Api.Controllers;

[ApiController]
public class AuthenticationController(AuthenticationService _authService) : ControllerBase {
    [HttpPost("[action]")]
    public async Task<IActionResult> Login([FromBody] string? GoogleIdToken) {
        Console.WriteLine($"AuthController.Login(string GoogleIdToken)");

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
                    return Unauthorized(result.Message ?? "Internal server error");

                case LogoutFailureCode.RefreshTokenNotFound:
                case LogoutFailureCode.RefreshTokenExpired:
                    Console.WriteLine($"AuthController.Logout() - Forbid({nameof(result.FailureCode)})");
                    return Forbid(result.Message ?? "Internal server error");
            }
        }
        
        return Ok(true);
    }

    [HttpPost("access")]
    public async Task<IActionResult> NewAccessToken() {
        Console.WriteLine("AuthController.Access()");

        if (!Request.Cookies.TryGetValue("refresh_token", out string? refresh_cookie)) {
            Console.WriteLine("AuthController.Access() - Unauthorized(MissingRefreshCookie)");
            return Unauthorized("Missing refresh cookie");
        }

        RefreshToken? token = await _authService.FindByCookie(refresh_cookie);
        if (token is null) {
            Console.WriteLine("AuthController.Access() - Unauthorized(MissingRefreshToken)");
            return Unauthorized("Missing refresh token");
        }

        if (token.Expires < DateTime.UtcNow) {
            Console.WriteLine("AuthController.Access() - Unauthorized(ExpiredRefreshToken)");
            return Unauthorized("Expired refresh token");
        }

        if (token.Revoked != null) {
            Console.WriteLine("AuthController.Access() - Unauthorized(RevokedRefreshToken)");
            return Unauthorized("Token already used or revoked");
        }

        string access_token = await _authService.GenerateAccessToken(token.User);

        return Ok(new { access_token });
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

        RefreshToken? token = await _authService.FindByCookie(refresh_cookie);
        if (token is null) {
            Console.WriteLine("AuthController.Refresh() - Unauthorized(NoRefreshToken)");
            return Unauthorized("Missing refresh token");
        }

        if (token.Expires < DateTime.UtcNow) {
            Console.WriteLine("AuthController.Refresh() - Unauthorized(ExpiredRefreshToken)");
            return Unauthorized("Expired refresh token");
        }

        if (token.Revoked != null) {
            Console.WriteLine("AuthController.Refresh() - Unauthorized(RevokedRefreshToken)");
            return Unauthorized("Token already used or revoked");
        }

        RefreshToken new_refresh_token = await _authService.GenerateRefreshToken(token.User, ipAddress);
        token.Revoked = DateTime.UtcNow;
        token.ReplacedByToken = new_refresh_token;
        await _authService.Update(token);

        _authService.CreateRefreshCookie(Response.Cookies, new_refresh_token);

        return Ok(true);
    }

    // TODO - Logout on all devices
}