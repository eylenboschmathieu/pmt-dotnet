using System.Security.Cryptography;
using System.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using Google.Apis.Auth;

using PMT.Data.Entities;
using PMT.Data.Repositories;

using Azure.Core;

namespace PMT.Services;

public enum LogoutFailureCode {
    RefreshTokenNotFound,
    RefreshTokenExpired,
    RefreshTokenRevoked
}

public sealed record LogoutResult {
    public bool Succeeded { get; init; }
    public LogoutFailureCode? FailureCode { get; init; }
    public string? Message { get; init; }

    public static LogoutResult Success() => new() { Succeeded = true };
    public static LogoutResult Fail(LogoutFailureCode code, string? message = null) => new() {
        Succeeded = false,
        FailureCode = code,
        Message = message
    };
}

public enum LoginFailureCode {
    BadToken,
    MissingIpAddress,
    GoogleVerification,
    Unauthorized,
    UserInactive,
}
    
public sealed record LoginResult {
    public bool Succeeded { get; init; }
    public string? AccessToken { get; init; }
    public RefreshToken? RefreshToken { get; init; }
    public LoginFailureCode? FailureCode { get; init; }
    public string? Message { get; init; }

    public static LoginResult Success(string accessToken, RefreshToken refreshToken) => new() {
        Succeeded = true,
        AccessToken = accessToken,
        RefreshToken = refreshToken
    };
    public static LoginResult Fail(LoginFailureCode code, string? message = null) => new() {
        Succeeded = false,
        FailureCode = code,
        Message = message
    };
}

public class AuthenticationService(IConfiguration _config, ITokenRepository _tokenRepository, IUserRepository _userRepository, IRoleRepository _roleRepository) {

    public async Task<LoginResult> LoginAsync(string? googleIdToken, IPAddress? ipAddress) {
        if (googleIdToken is null || googleIdToken == string.Empty)
            return LoginResult.Fail(LoginFailureCode.BadToken);

        if (ipAddress is null)
            return LoginResult.Fail(LoginFailureCode.MissingIpAddress);

        //=== Verify user with Google ===//
        GoogleJsonWebSignature.Payload payload;
        try {
            payload = await GoogleJsonWebSignature.ValidateAsync(googleIdToken, new GoogleJsonWebSignature.ValidationSettings {
                Audience = [_config["Authentication:Google:ClientId"]]
            });
        }
        catch (InvalidJwtException) {
            Console.WriteLine("AuthController.Login() - Unauthorized(BadToken)");
            return LoginResult.Fail(LoginFailureCode.GoogleVerification, "Invalid JWT token"); // Unauthorized
        }
        // ============================= //

        if (!payload.EmailVerified)
            return LoginResult.Fail(LoginFailureCode.GoogleVerification, "Email not verified");

        // If the user logged in before, the google id has previously been associated with this user, google id is null otherwise
        User? user = await _userRepository.FindByGoogleId(payload.Subject);
        if (user is null) {
            // First login
            user = await _userRepository.FindByEmail(payload.Email);
            if (user is null)
                return LoginResult.Fail(LoginFailureCode.Unauthorized, "Email not registered"); // Unauthorized

            Console.WriteLine($"First login ({payload.Name})!");
            user.Name = payload.Name;
            user.GoogleId = payload.Subject;
            await _userRepository.UpdateAsync(user);
        }

        if (!user.Active) {
            Console.WriteLine("AuthController.Login() - Unauthorized(UserInactive)");
            return LoginResult.Fail(LoginFailureCode.UserInactive);
        }

        string access_token = await GenerateAccessToken(user);
        RefreshToken refresh_token = await GenerateRefreshToken(user, ipAddress);

        return LoginResult.Success(access_token, refresh_token);
    }

    public async Task<LogoutResult> LogoutAsync(string refresh_cookie) {
        RefreshToken? token = await _tokenRepository.FindByCookieAsync(refresh_cookie);
        if (token is null)
            return LogoutResult.Fail(LogoutFailureCode.RefreshTokenNotFound, "Missing refresh token");

        if (token.Expires < DateTime.UtcNow)
            return LogoutResult.Fail(LogoutFailureCode.RefreshTokenExpired, "Expired refresh token");

        if (token.Revoked is not null)
            return LogoutResult.Fail(LogoutFailureCode.RefreshTokenRevoked, "Revoked refresh token");

        token.Revoked = DateTime.UtcNow;  // Invalidate refresh token
        await _tokenRepository.UpdateAsync(token);

        return LogoutResult.Success();
    }

    /* Returns serialized JwtSecurityTokenHandler containing claims */
    public async Task<string> GenerateAccessToken(User user) {
        // 1. Create claims (identity + roles)
        var claims = new List<Claim> {
            new (JwtRegisteredClaimNames.Sub, user.Id.ToString()),  // Subject
            new (JwtRegisteredClaimNames.Name, user.Name ?? throw new Exception("Username cannot be null")),
            new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),  // Token Id
        };

        // Add role claims
        // Instead of using dotnet built in claim names, use our own as we're using them on the front-end to store data about
        // the currently logged in user. The dotnet ones are long-ass URI's.
        foreach (var role in await _roleRepository.FindByUser(user)) {
            claims.Add(new Claim("Roles", role.Name));
        }

        // 2. Define key and signing credentials
        SymmetricSecurityKey key = new (Encoding.UTF8.GetBytes(_config["App:Secret"]!));
        SigningCredentials creds = new (key, SecurityAlgorithms.HmacSha256);

        // 3. Create the token
        JwtSecurityToken token = new (
            issuer: _config["App:Issuer"],
            audience: _config["App:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds
        );

        // 4. Serialize token to string
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<RefreshToken> GenerateRefreshToken(User user, IPAddress IpAddress) {
        var refresh_token = new RefreshToken {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            Expires = DateTime.UtcNow.AddDays(7),
            UserId = user.Id,
            IpAddress = IpAddress,
            ReplacedByToken = null
        };

        // Need to hash the tokens before storing them in db.
        return await _tokenRepository.CreateAsync(refresh_token);
    }

    public async Task<RefreshToken?> FindByCookie(string refresh_cookie) {
        return await _tokenRepository.FindByCookieAsync(refresh_cookie);
    }

    public async Task<RefreshToken?> Update(RefreshToken refresh_token) {
        RefreshToken? token = await _tokenRepository.GetAsync(refresh_token.Id);
        if (token is null)
            throw new DbUpdateException();

        token.Token = refresh_token.Token;
        token.IpAddress = refresh_token.IpAddress;
        token.Created = refresh_token.Created;
        token.Expires = refresh_token.Expires;
        token.ReplacedByToken = refresh_token.ReplacedByToken;
        token.User = refresh_token.User;
        token.Revoked = refresh_token.Revoked;

        await _tokenRepository.UpdateAsync(refresh_token);

        return refresh_token;
    }

    public void CreateRefreshCookie(IResponseCookies cookies, RefreshToken refreshToken) {
        cookies.Append("refresh_token", refreshToken.Token, new CookieOptions {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Expires = refreshToken.Expires,
            Path = "/"
        });
    }
}
