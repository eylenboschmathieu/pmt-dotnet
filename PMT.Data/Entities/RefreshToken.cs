using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;

using Microsoft.EntityFrameworkCore;

namespace PMT.Data.Entities;

/*
    A bunch of these fields are here for analysis more than anything
    Should probably make a page showing this stuff
*/

[Index(nameof(Token))]
public class RefreshToken {
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }  // Primary Key

    public string Token { get; set; } = string.Empty;  // The token itself
    public DateTime Expires { get; set; }  // Time of token expiration
    public DateTime Created { get; set; } = DateTime.UtcNow;  // Time of token creation
    public DateTime? Revoked { get; set; }  // Time of token revocation
    public required IPAddress IpAddress { get; set; }  // Ip address of who requested the token

    // Relationships
    [ForeignKey(nameof(User))]
    public int UserId { get; set; }  // User for which the token was issued.
    public User User { get; set; } = null!;

    [ForeignKey(nameof(ReplacedByToken))]
    public int? ReplacedByTokenId { get; set; }
    public RefreshToken? ReplacedByToken { get; set; }
}

// The following is from chatgpt; beware.

/*
🔐 Token
    The actual refresh token value you send to the client (as an HttpOnly cookie).
    It’s usually a random, cryptographically secure string (e.g., 64+ characters).
    You’ll use this to match incoming refresh requests to the database.

    👉 Best practice:
        Store only a hashed version (like passwords) if you want to be extra secure, so even if the DB leaks, tokens can’t be reused.

📅 Expires
    The absolute expiration date/time for this refresh token.
    Once expired, it’s invalid — even if it hasn’t been revoked.
    Common values: 7 days, 14 days, or 30 days.

🕓 Created
    When the token was created. Useful for auditing and debugging.

🌐 CreatedByIp
    IP address that created the token (optional, but useful for logging suspicious activity).
    Example: if a refresh request comes from a completely different country/IP, you might want to reject it.

🚫 Revoked and RevokedByIp
    If you explicitly invalidate a refresh token (e.g., user logs out or token reuse detected), set Revoked to the current timestamp.
    RevokedByIp logs which IP triggered the revocation.

🔁 ReplacedByToken
    If a refresh token is rotated (new one issued when refreshing), this field links the old token to the new one.
    This helps prevent token reuse attacks — if an old refresh token is used again after rotation, you can detect and revoke all related tokens.

👤 UserId + User
    The user who owns the token (foreign key to your User table).
    Needed to identify which user the refresh token belongs to.


🧠 Putting it all together
    Flow:
        1. User logs in → you generate:
            Access token (short-lived, e.g. 15m)
            Refresh token (long-lived, e.g. 7d)
        2. You store the refresh token in the database (with metadata).
        3. Send it to the client (HttpOnly cookie).
        4. When client requests a refresh:
            You find the token in DB.
            Check:
                Is it expired? (Expires < now)
                Is it revoked? (Revoked != null)
            If valid → issue a new access token and a new refresh token.
            Update DB:
                Mark the old refresh token as revoked.
                Save the new one, with ReplacedByToken pointing back.


🧰 Optional fields (depending on your needs)
    Field           Purpose
    ---------------------------------------------------------------------------
    DeviceInfo      Track which device/browser the token belongs to
    IsActive        Return true if not expired/revoked (computed property)
    UserAgent       Store client user agent for debugging or analytics
    RefreshCount    Track how many times a refresh occurred
*/