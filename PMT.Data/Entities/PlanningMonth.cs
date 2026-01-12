using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMT.Data.Entities;

public class PlanningMonth {
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public DateOnly Date { get; set; }

    [Required]
    public bool Locked { get; set; } = false;
}
