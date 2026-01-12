using System.Security.Cryptography;
using System.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using PMT.Data.Entities;
using PMT.Data.Repositories;
using Microsoft.AspNetCore.Http;

namespace PMT.Services;

public class TokenService(ITokenRepository _tokenRepository, IRoleRepository _roleRepository) {

    /* Returns serialized JwtSecurityTokenHandler containing claims */
    public async Task<string> GenerateAccessToken(User user, IConfiguration config) {
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
        SymmetricSecurityKey key = new (Encoding.UTF8.GetBytes(config["App:Secret"]!));
        SigningCredentials creds = new (key, SecurityAlgorithms.HmacSha256);

        // 3. Create the token
        JwtSecurityToken token = new (
            issuer: config["App:Issuer"],
            audience: config["App:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds
        );

        // 4. Serialize token to string
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<RefreshToken> GenerateRefreshToken(User user, IPAddress IpAddress, string? old_token = null) {
        var refresh_token = new RefreshToken {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            Expires = DateTime.UtcNow.AddDays(7),
            UserId = user.Id,
            IpAddress = IpAddress,
            ReplacedByToken = old_token is null ? null : await _tokenRepository.FindByTokenAsync(old_token)
        };

        // Need to hash the tokens before storing them in db.

        return await _tokenRepository.AddAsync(refresh_token);
    }

    public async Task<RefreshToken?> FindByToken(string refresh_token) {
        return await _tokenRepository.FindByTokenAsync(refresh_token);
    }

    public async Task<RefreshToken?> Update(RefreshToken refresh_token) {
        return await _tokenRepository.UpdateAsync(refresh_token);
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
