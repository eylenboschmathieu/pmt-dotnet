using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace PMT.Data.Entities;

[Index(nameof(UserId), nameof(ShiftId), IsUnique = true)]
public class UserShift {
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [ForeignKey(nameof(User))]
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    [ForeignKey(nameof(Shift))]
    public int ShiftId { get; set; }
    public Shift Shift { get; set; } = null!;

    public bool Confirmed { get; set; }
}
