using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PMT.Data.Entities;

public class Role {
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(48)]
    public string Name { get; set; } = string.Empty;

    [ForeignKey(nameof(Parent))]
    public int? ParentId { get; set; }
    public Role? Parent { get; set; }

    /// <summary>
    /// This value indicates how far down the tree of children we can assign roles from.
    /// 
    /// <para>DelegationDepth = 0 → can assign no descendants</para>
    /// <para>DelegationDepth = 1 → can assign direct children only</para>
    /// <para>DelegationDepth = 2 → can assign children and grandchildren</para>
    /// <para>DelegationDepth = n → can assign n-th level of descendants</para>
    /// <para>DelegationDepth = max → effectively Admin behavior</para>
    /// 
    /// </summary>
    [Required]
    public int DelegationDepth { get; set; }

    public ICollection<User> Users { get; set; } = [];
}