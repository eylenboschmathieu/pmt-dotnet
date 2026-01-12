using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace PMT.Data.Entities;

[Index(nameof(From), nameof(To), IsUnique = true)]
public class Shift {
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public DateTime From { get; set; }

    [Required]
    public DateTime To { get; set; }
}
