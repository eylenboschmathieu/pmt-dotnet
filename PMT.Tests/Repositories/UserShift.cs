using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using PMT.Data;
using PMT.Data.Entities;
using PMT.Data.Repositories;

namespace PMT.Tests.Repository;

[TestClass]
public class UserShiftTests {
    private UserShiftRepository _repo = null!;

    [TestInitialize]
    public async Task Init() {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"InMemoryDatabase{Guid.NewGuid()}")
            .Options;

        var context = new ApplicationDbContext(options);
        User[] users = [
            new User {
                Id = 1,
                Name = "John",
                Email = "john@gmail.com",
                GoogleId = "1234",
                Active = true
            },
            new User {
                Id = 2,
                Name = "Jane",
                Email = "jane@gmail.com",
                GoogleId = "5678",
                Active = true
            }
        ];

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

        UserShift[] userShifts = [
            new() { Id = 1, ShiftId = 1, UserId = 1, Planned = true },
            new() { Id = 2, ShiftId = 3, UserId = 2, Planned = true },
            new() { Id = 3, ShiftId = 6, UserId = 2 },
            new() { Id = 4, ShiftId = 2, UserId = 1 },
            new() { Id = 5, ShiftId = 5, UserId = 1 },
            new() { Id = 6, ShiftId = 4, UserId = 2, Planned = true },
            new() { Id = 7, ShiftId = 8, UserId = 1, Planned = true },
            new() { Id = 8, ShiftId = 7, UserId = 2 }
        ];

        PlanningMonth[] months = [
            new() { Id = 1, Date = new(2025, 9, 1), Locked = true },
            new() { Id = 2, Date = new(2025, 10, 1), Locked = true },
            new() { Id = 3, Date = new(2025, 11, 1), Locked = false },
            new() { Id = 4, Date = new(2025, 12, 1), Locked = false }
        ];
        
        await context.Users.AddRangeAsync(users);
        await context.Shifts.AddRangeAsync(shifts);
        await context.UserShifts.AddRangeAsync(userShifts);

        await context.SaveChangesAsync();
        _repo = new UserShiftRepository(context);
    }

    [TestMethod]
    public async Task CreateNewUserShift() {
        UserShift s = await _repo.CreateAsync(new() {
            ShiftId = 8,
            UserId = 2,
        });

        Assert.AreEqual(9, s.Id);
    }

    [TestMethod]
    public async Task UpdateUserShift() {
        UserShift? s = await _repo.GetAsync(4);
        Assert.IsNotNull(s);
        s.Planned = true;

        Assert.IsTrue(await _repo.UpdateAsync(s));
    }

    [TestMethod]
    public async Task DeleteUserShift() {
        Assert.IsTrue(await _repo.DeleteAsync(8));
        UserShift? s = await _repo.GetAsync(8);
        Assert.IsNull(s);
    }

    [TestMethod]
    public async Task GetAllUserShifts() {
        IEnumerable<UserShift> s = await _repo.GetAllAsync();
        Assert.HasCount(8, s);
    }

    [TestMethod]
    public async Task GetUserShift() {
        UserShift? s = await _repo.GetAsync(8);
        Assert.IsNotNull(s);
    }

    [TestMethod]
    public async Task GetPlannedShiftsForUserBetweenTwoDates() {
        IEnumerable<Shift> s = await _repo.GetPlannedShifts(1, new(2025, 1, 1), new(2025, 12, 1));
        Assert.HasCount(2, s);

        s = await _repo.GetPlannedShifts(1, new(2025, 1, 1), new(2025, 11, 1));
        Assert.HasCount(1, s);
    }

    [TestMethod]
    public async Task GetMonthsThatStillNeedToBePlanned() {
        // Is calculated based on DateTime.UTCNow, makes testing this a bit ikky.

        // IEnumerable<MonthsDTO> months = await _repo.GetPlanningMonths();
        // Assert.HasCount(1, months);
    }

    [TestMethod]
    public async Task GetOverviewData() {
        IEnumerable<OverviewData> data = await _repo.GetOverviewData(new(2026, 1, 1));
        Assert.HasCount(2, data);
    }

    [TestMethod]
    public async Task GetTheTotalHoursRequestedForEachUser() {
        Dictionary<int, int> dict = await _repo.GetRequestedHoursForYear(new(2026, 1, 1));
        Assert.AreEqual(20, dict[1]);
        Assert.AreEqual(13, dict[2]);
    }

    [TestMethod]
    public async Task GetAllMonthsThatAcceptShiftRequests() {
        // Is calculated based on DateTime.UTCNow, making testing this a bit ikky

        // IEnumerable<DateOnly> months = await _repo.GetRequestedMonths();
        // Assert.HasCount(1, months);
    }

    [TestMethod]
    public async Task f() {
        // var shifts = await _repo.GetRequestsForDay(new(2025, 11, 1));
        // Assert.HasCount(5, shifts);

        // This one is a bit tricky, figure it out later
    }
}
