using Microsoft.EntityFrameworkCore;

using PMT.Data;
using PMT.Data.Entities;
using PMT.Data.Repositories;

namespace PMT.Tests.Repository;

[TestClass]
public class ShiftTests {
    private ShiftRepository _repo = null!;

    [TestInitialize]
    public async Task Init() {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"InMemoryDatabase{Guid.NewGuid()}")
            .Options;

        var context = new ApplicationDbContext(options);
        Shift[] shifts = [
            new() {
                Id = 1,
                From = DateTime.SpecifyKind(new(2025, 11, 1, 6, 0, 0), DateTimeKind.Utc),
                To = DateTime.SpecifyKind(new(2025, 11, 1, 9, 0, 0), DateTimeKind.Utc)
            },
            new() {
                Id = 2,
                From = DateTime.SpecifyKind(new(2025, 11, 1, 9, 0, 0), DateTimeKind.Utc),
                To = DateTime.SpecifyKind(new(2025, 11, 1, 12, 0, 0), DateTimeKind.Utc)
            },
            new() {
                Id = 3,
                From = DateTime.SpecifyKind(new(2025, 11, 1, 12, 0, 0), DateTimeKind.Utc),
                To = DateTime.SpecifyKind(new(2025, 11, 1, 15, 0, 0), DateTimeKind.Utc)
            },
            new() {
                Id = 4,
                From = DateTime.SpecifyKind(new(2025, 11, 1, 15, 0, 0), DateTimeKind.Utc),
                To = DateTime.SpecifyKind(new(2025, 11, 1, 19, 0, 0), DateTimeKind.Utc)
            },
            new() {
                Id = 5,
                From = DateTime.SpecifyKind(new(2025, 11, 1, 19, 0, 0), DateTimeKind.Utc),
                To = DateTime.SpecifyKind(new(2025, 11, 2, 6, 0, 0), DateTimeKind.Utc)
            },
            new() {
                Id = 6,
                From = DateTime.SpecifyKind(new(2025, 11, 2, 6, 0, 0), DateTimeKind.Utc),
                To = DateTime.SpecifyKind(new(2025, 11, 2, 9, 0, 0), DateTimeKind.Utc)
            },
            new() {
                Id = 7,
                From = DateTime.SpecifyKind(new(2025, 11, 2, 9, 0, 0), DateTimeKind.Utc),
                To = DateTime.SpecifyKind(new(2025, 11, 2, 12, 0, 0), DateTimeKind.Utc)
            },
            new() {
                Id = 8,
                From = DateTime.SpecifyKind(new(2025, 11, 2, 12, 0, 0), DateTimeKind.Utc),
                To = DateTime.SpecifyKind(new(2025, 11, 2, 15, 0, 0), DateTimeKind.Utc)
            },
        ];

        await context.Shifts.AddRangeAsync(shifts);

        await context.SaveChangesAsync();
        _repo = new ShiftRepository(context);
    }

    [TestMethod]
    public async Task FindShift() {
        Shift? s = await _repo.GetAsync(1);
        Assert.IsNotNull(s);
        Assert.AreEqual(1, s.Id);
    }

    [TestMethod]
    public async Task GetAllShifts() {
        IEnumerable<Shift> s = await _repo.GetAllAsync();
        Assert.HasCount(8, s);
    }

    [TestMethod]
    public async Task CreateNewShift() {
        Shift s = await _repo.CreateAsync(new() {
            From = DateTime.SpecifyKind(new(2025, 11, 2, 15, 0, 0), DateTimeKind.Utc),
            To = DateTime.SpecifyKind(new(2025, 11, 2, 19, 0, 0), DateTimeKind.Utc)
        });
        Assert.AreEqual(9, s.Id);
    }
}
