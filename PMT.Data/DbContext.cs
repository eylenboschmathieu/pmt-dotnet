using Microsoft.EntityFrameworkCore;

using PMT.Data.Entities;

namespace PMT.Data;

public class ApplicationDbContext : DbContext {
    private readonly bool _testMode = false;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {
        //_testMode = testMode;
    }

    // DbSet properties for your entities
    public DbSet<Role> Roles { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Shift> Shifts { get; set; }
    public DbSet<UserShift> UserShifts { get; set; }
    public DbSet<PlanningMonth> PlanningMonths { get; set; }

    // Override OnModelCreating to configure the model
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        var roles = modelBuilder.Entity<Role>();
        var users = modelBuilder.Entity<User>();
        var refreshTokens = modelBuilder.Entity<RefreshToken>();
        var shifts = modelBuilder.Entity<Shift>();
        var userShifts = modelBuilder.Entity<UserShift>();
        var planningMonths = modelBuilder.Entity<PlanningMonth>();

        users.HasMany(e => e.Roles)
            .WithMany(e => e.Users)
            .UsingEntity("UserRole",
                e => e.HasOne(typeof(Role)).WithMany().HasForeignKey("RoleId"),
                e => e.HasOne(typeof(User)).WithMany().HasForeignKey("UserId"),
                e => e.HasKey("UserId", "RoleId")
            );

        refreshTokens.HasOne(e => e.ReplacedByToken)
            .WithMany()
            .HasForeignKey(e => e.ReplacedByTokenId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seeding
        if (!_testMode) {
            roles.HasData([
                new() { Id = 1, Name = "Admin" },
                new() { Id = 2, Name = "Manager" },
                new() { Id = 3, Name = "Paramedic" },
                new() { Id = 4, Name = "Intern" }
            ]);
        }    
    }
}