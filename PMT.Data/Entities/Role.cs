using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMT.Data.Entities;

public class Role {
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(48)]
    public string Name { get; set; } = string.Empty;

    public ICollection<User> Users { get; set; } = [];
}