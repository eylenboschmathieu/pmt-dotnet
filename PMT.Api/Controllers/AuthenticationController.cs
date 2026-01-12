using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Google.Apis.Auth;

using PMT.Data.Entities;
using PMT.Services;
using System.Net;

namespace PMT.Api.Controllers;

[ApiController]
public class AuthenticationController(IConfiguration _config, UserService _userService, TokenService _tokenService) : ControllerBase {

    [HttpPost("[action]")]
    public async Task<IActionResult> Login([FromBody] string GoogleIdToken) {
        Console.WriteLine($"AuthController.Login(string GoogleIdToken)");

        //=== Verify user with Google ===//
        GoogleJsonWebSignature.Payload payload;
        try {
            payload = await GoogleJsonWebSignature.ValidateAsync(GoogleIdToken, new GoogleJsonWebSignature.ValidationSettings {
                Audience = [_config["Authentication:Google:ClientId"]]
            });
        }
        catch (InvalidJwtException) {
            Console.WriteLine("AuthController.Login() - Unauthorized(BadToken)");
            return Unauthorized();
        }
        // ============================= //

        if (payload.EmailVerified) {
            // If the user logged in before, the google id has previously been associated with this user, google id is null otherwise
            User? user = await _userService.FindByGoogleId(payload.Subject);
            if (user is null) {
                // First login
                user = await _userService.FindByEmail(payload.Email);
                if (user is null) {
                    Console.Write($"AuthController.Login() - Unauthorized({payload.Email})");
                    return Unauthorized();
                }

                Console.WriteLine($"First login ({payload.Name})!");
                user.Name = payload.Name;
                user.GoogleId = payload.Subject;
                await _userService.Update(user);
            }

            if (!user.Active) {
                Console.WriteLine("AuthController.Login() - Unauthorized(UserInactive)");
                return Unauthorized();
            }

            IPAddress? ipAddress = Request.HttpContext.Connection.RemoteIpAddress;
            if (ipAddress is null) {
                Console.WriteLine("AuthController.Login() - Unauthorized(BadIpAddress)");
                return Unauthorized();
            }
            if (GoogleIdToken is null || GoogleIdToken == string.Empty) {
                Console.WriteLine("AuthController.Login() - Unauthorized(BadRequest)");
                return Unauthorized();
            }

            string access_token = await _tokenService.GenerateAccessToken(user, _config);
            RefreshToken refresh_token = await _tokenService.GenerateRefreshToken(user, ipAddress);

            _tokenService.CreateRefreshCookie(Response.Cookies, refresh_token);

            return Ok(new { access_token });
        }

        return Unauthorized();
    }

    [Authorize]
    [HttpPost("[action]")]
    public async Task<IActionResult> Logout() {
        Console.WriteLine("Logout");
        if (!Request.Cookies.TryGetValue("refresh_token", out string? refresh_token)) {
            Console.WriteLine("AuthController.Refresh() - Forbid(NoRefreshToken)");
            return Forbid("Missing refresh token");  // No refresh token was found
        }

        RefreshToken? token = await _tokenService.FindByToken(refresh_token);
        if (token is null) {
            Console.WriteLine("AuthController.Refresh() - Forbid(InvalidOrMissingRefreshToken)");
            return Forbid("Invalid, missing, or expired refresh token");
        }

        token.Revoked = DateTime.UtcNow;  // Invalidate refresh token
        await _tokenService.Update(token);

        if (token.Expires < DateTime.UtcNow) {
            Console.WriteLine("AuthController.Refresh() - Forbid(ExpiredRefreshToken)");
            return Forbid("Expired refresh token");
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

        RefreshToken? token = await _tokenService.FindByToken(refresh_cookie);
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

        string access_token = await _tokenService.GenerateAccessToken(token.User, _config);

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

        if (!Request.Cookies.TryGetValue("refresh_token", out string? refresh_token)) {
            Console.WriteLine("AuthController.Refresh() - Unauthorized(NoRefreshCookie)");
            return Unauthorized("No refresh cookie");  // No refresh token was found
        }

        RefreshToken? token = await _tokenService.FindByToken(refresh_token);
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

        RefreshToken new_refresh_token = await _tokenService.GenerateRefreshToken(token.User, ipAddress);
        token.Revoked = DateTime.UtcNow;
        token.ReplacedByToken = new_refresh_token;
        await _tokenService.Update(token);

        _tokenService.CreateRefreshCookie(Response.Cookies, new_refresh_token);

        return Ok(true);
    }

    // TODO - Logout on all devices
}