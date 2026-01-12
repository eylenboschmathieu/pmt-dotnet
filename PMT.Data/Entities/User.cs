using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace PMT.Data.Entities;

[Index(nameof(Email), IsUnique = true)]
public class User {
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [StringLength(256)]
    public string? Name { get; set; }

    [StringLength(256)]
    public string? GoogleId { get; set; }

    [Required]
    [StringLength(256)]
    public string Email { get; set; } = null!;

    public bool Active { get; set; }

    public ICollection<Role> Roles { get; set; } = [];

    [DeleteBehavior(DeleteBehavior.Cascade)]
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];

    // Relationships
    [ForeignKey(nameof(User))]
    public int? CreatedById { get; set; }  // Creator of this user.
    public User? CreatedBy { get; set; } = null;

    public override string ToString() {
        return $"{Name ?? "null"}, {Email}, {CreatedBy!.Name ?? "admin"}";
    }
}