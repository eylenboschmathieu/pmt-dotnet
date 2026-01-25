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

namespace PMT.Services;

public abstract record Result {
    protected const string ISE = "Internal Server Error";
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
}

public enum LogoutFailureCode {
    RefreshTokenNotFound,
    RefreshTokenExpired,
    RefreshTokenRevoked
}

public sealed record LogoutResult : Result {
    public LogoutFailureCode? FailureCode { get; init; }

    public static LogoutResult Success() => new() { Succeeded = true };
    public static LogoutResult Fail(LogoutFailureCode code, string? message = ISE) => new() {
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
    
public sealed record LoginResult : Result {
    public string? AccessToken { get; init; }
    public RefreshToken? RefreshToken { get; init; }
    public LoginFailureCode? FailureCode { get; init; }

    public static LoginResult Success(string accessToken, RefreshToken refreshToken) => new() {
        Succeeded = true,
        AccessToken = accessToken,
        RefreshToken = refreshToken
    };
    public static LoginResult Fail(LoginFailureCode code, string? message = ISE) => new() {
        Succeeded = false,
        FailureCode = code,
        Message = message
    };
}

public enum RefreshTokenFailureCode {
    Missing,
    Expired,
    Revoked
}

public sealed record AccessResult : Result {
    /// <summary>Access Token</summary>
    public string? AccessToken { get; init; }
    public RefreshTokenFailureCode? FailureCode { get; init; }
    public static AccessResult Success(string accessToken) => new() { Succeeded = true, AccessToken = accessToken };
    public static AccessResult Fail(RefreshTokenFailureCode code, string? message = ISE) => new() {
        Succeeded = false,
        FailureCode = code,
        Message = message
    };
}

public sealed record RefreshResult : Result {
    /// <summary>Refresh Token</summary>
    public RefreshToken? RefreshToken { get; init; }
    public RefreshTokenFailureCode? FailureCode { get; init; }
    public static RefreshResult Success(RefreshToken refreshToken) => new() { Succeeded = true, RefreshToken = refreshToken };
    public static RefreshResult Fail(RefreshTokenFailureCode code, string? message = ISE) => new() {
        Succeeded = false,
        FailureCode = code,
        Message = message
    };
}

public class AuthenticationService(IConfiguration _config,
        ITokenRepository _tokenRepository,
        IUserRepository _userRepository,
        IRoleRepository _roleRepository) {

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

    public async Task<LogoutResult> FullLogoutAsync(string refresh_cookie) {
        RefreshToken? refresh_token = await _tokenRepository.FindByCookieAsync(refresh_cookie);
        if (refresh_token is null)
            return LogoutResult.Fail(LogoutFailureCode.RefreshTokenNotFound, "Missing refresh token");

        if (refresh_token.Expires < DateTime.UtcNow)
            return LogoutResult.Fail(LogoutFailureCode.RefreshTokenExpired, "Expired refresh token");

        if (refresh_token.Revoked is not null)
            return LogoutResult.Fail(LogoutFailureCode.RefreshTokenRevoked, "Revoked refresh token");

        await _tokenRepository.RevokeUser(refresh_token.UserId);

        return LogoutResult.Success();
    }

    public async Task<AccessResult> CreateAccessTokenAsync(string refresh_cookie) {
        RefreshToken? token = await _tokenRepository.FindByCookieAsync(refresh_cookie);
        if (token is null)
            return AccessResult.Fail(RefreshTokenFailureCode.Missing, "Missing refresh token");

        if (token.Expires < DateTime.UtcNow)
            return AccessResult.Fail(RefreshTokenFailureCode.Expired, "Expired refresh token");

        if (token.Revoked != null)
            return AccessResult.Fail(RefreshTokenFailureCode.Revoked, "Token already used or revoked");
        
        string access_token = await GenerateAccessToken(token.User);

        return AccessResult.Success(access_token);
    }

    public async Task<RefreshResult> CreateRefreshTokenAsync(string refresh_cookie, IPAddress ipAddress) {
        RefreshToken? refresh_token = await _tokenRepository.FindByCookieAsync(refresh_cookie);
        if (refresh_token is null)
            return RefreshResult.Fail(RefreshTokenFailureCode.Missing, "Missing refresh token");

        if (refresh_token.Expires < DateTime.UtcNow)
            return RefreshResult.Fail(RefreshTokenFailureCode.Expired, "Expired refresh token");

        if (refresh_token.Revoked != null)
            return RefreshResult.Fail(RefreshTokenFailureCode.Revoked, "Token already used or revoked");

        RefreshToken new_refresh_token = await GenerateRefreshToken(refresh_token.User, ipAddress);
        refresh_token.Revoked = DateTime.UtcNow;
        refresh_token.ReplacedByToken = new_refresh_token;
        await _tokenRepository.UpdateAsync(refresh_token);

        return RefreshResult.Success(new_refresh_token);
    }

    /* Returns serialized JwtSecurityTokenHandler containing claims */
    private async Task<string> GenerateAccessToken(User user) {
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

    private async Task<RefreshToken> GenerateRefreshToken(User user, IPAddress IpAddress) {
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
