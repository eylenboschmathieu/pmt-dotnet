using Microsoft.EntityFrameworkCore;

using PMT.Data.Entities;

namespace PMT.Data.Repositories;

public class ShiftTime {
    public TimeOnly From { get; set; }
    public TimeSpan Duration { get; set; }
}

public class UserShiftRepository(ApplicationDbContext _dbContext) : IUserShiftRepository {
    private static readonly ShiftTime[] ShiftHours = [  // We're storing hours in UTC time, so make sure the shift hours are in UTC time too
        new ShiftTime { From = new TimeOnly(5, 0), Duration = new TimeSpan(3, 0, 0) },
        new ShiftTime { From = new TimeOnly(8, 0), Duration = new TimeSpan(3, 0, 0) },
        new ShiftTime { From = new TimeOnly(11, 0), Duration = new TimeSpan(3, 0, 0) },
        new ShiftTime { From = new TimeOnly(14, 0), Duration = new TimeSpan(4, 0, 0) },
        new ShiftTime { From = new TimeOnly(18, 0), Duration = new TimeSpan(11, 0, 0) }
    ];

    public ShiftTime[] GetShiftHours() {
        return ShiftHours;
    }

    public async Task<UserShift> AddAsync(UserShift entity) {
        _dbContext.UserShifts.Add(entity);
        await _dbContext.SaveChangesAsync();

        return entity;
    }

    public async Task<bool> DeleteAsync(int id) {
        UserShift? rs = await _dbContext.UserShifts.FindAsync(id);

        if (rs is null)
            return false;

        _dbContext.UserShifts.Remove(rs!);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<UserShift>> FindAllAsync() {
        return await _dbContext.UserShifts.AsNoTracking().ToListAsync();
    }

    public async Task<UserShift?> FindByIdAsync(int id) {
        return await _dbContext.UserShifts.FindAsync(id);
    }

    public async Task<ICollection<Shift>> GetConfirmedShifts(int userId, DateOnly from, DateOnly to) {
        return await _dbContext.UserShifts
            .Where(e =>
                e.UserId == userId &&
                DateOnly.FromDateTime(e.Shift.From) >= from &&
                DateOnly.FromDateTime(e.Shift.To) <= to &&
                e.Confirmed)
            .Select(e => e.Shift)
            .OrderBy(e => e.From)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<ICollection<MonthsDTO>> GetPlanningMonths() {
        return await _dbContext.PlanningMonths
            .Select(e => new MonthsDTO { Date = e.Date, Locked = e.Locked } )
            .OrderByDescending(e => e.Date)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<ICollection<DateOnly>> GetRequestMonths(int userId) {
        return await _dbContext.UserShifts
            .Where(e => e.UserId == userId)
            .Select(e => DateOnly.FromDateTime(e.Shift.From))
            .Distinct()
            .OrderByDescending(e => e)
            .ToListAsync();
    }

    public async Task<List<IGrouping<DateTime, UserShift>>> GetRequestsForDay(DateOnly date) {
        return await _dbContext.UserShifts
            .Where(e => DateOnly.FromDateTime(e.Shift.From) == date)
            .Include(e => e.User).Include(e => e.User.Roles)
            .GroupBy(e => e.Shift.From)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<ICollection<UserShift>> GetUserRequestsForDay(int userId, DateOnly date) {
        return await _dbContext.UserShifts
            .Where(e => e.UserId == userId && DateOnly.FromDateTime(e.Shift.From) == date)
            .Include(e => e.Shift)
            .OrderByDescending(e => e.Shift.From)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> LockMonth(DateOnly date, bool locked) {
        PlanningMonth pm = await _dbContext.PlanningMonths.Where(e => e.Date == date).FirstAsync();

        if (pm is not null) {
            pm.Locked = locked;
            _dbContext.PlanningMonths.Update(pm);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<UserShift?> UpdateAsync(UserShift entity) {
        var rs = await _dbContext.UserShifts.FindAsync(entity.Id);
        if (rs is null)
            return null;

        rs.UserId = entity.UserId;
        rs.ShiftId = entity.ShiftId;
        await _dbContext.SaveChangesAsync();

        return rs;
    }

    public async Task<bool> CreateRequest(int userId, DateTime date) {
        Shift? shift = await _dbContext.Shifts.Where(e => e.From.Equals(date)).FirstOrDefaultAsync();

        if (shift is null) {
            TimeSpan to = ShiftHours.Where(e => TimeOnly.FromDateTime(date) == e.From).Select(e => e.Duration).FirstOrDefault();
            shift = new Shift {
                From = date,
                To = date.Add(to)
            };
            _dbContext.Shifts.Add(shift);
            await _dbContext.SaveChangesAsync();
        }

        _dbContext.UserShifts.Add(new UserShift {
            UserId = userId,
            ShiftId = shift.Id,
            Confirmed = false
        });
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteRequest(int userId, DateTime shift) {
        UserShift? userShift = await _dbContext.UserShifts.Include(e => e.Shift).Where(e => e.UserId == userId && e.Shift.From == shift).FirstOrDefaultAsync();

        if (userShift is null)
            return false;

        _dbContext.UserShifts.Remove(userShift);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ConfirmPlanningForShift(bool confirm, int shiftId) {
        UserShift? shift = await _dbContext.UserShifts.Where(e => e.Id == shiftId).FirstOrDefaultAsync();

        if (shift is not null) {
            shift.Confirmed = confirm;

            _dbContext.UserShifts.Update(shift);
            await _dbContext.SaveChangesAsync();
            return true;   
        } else
            Console.WriteLine("Shift not found!");

        return false;
    }

    // Return overview data from {date}, going back 12 months
    public async Task<List<OverviewData>> GetOverviewData(DateOnly date) {
        DateTime to = new DateTime(date.Year, date.Month, 1).AddMonths(1).AddDays(-1);
        DateTime from = new DateTime(date.Year, date.Month, 1).AddMonths(-11);

        var query = await _dbContext.UserShifts
            .Where(e => e.Confirmed)
            .Join(_dbContext.Shifts, us => us.ShiftId, s => s.Id, (us, s) => new {
                us.UserId,
                Shift = s
            })
            .Join(_dbContext.Users, x => x.UserId, u => u.Id, (x, u) => new {
                x.UserId,
                UserName = u.Name,
                x.Shift.From,
                x.Shift.To,
            })
            .Where(x => x.From >= from && x.To <= to).ToListAsync();

        var grouped = query.GroupBy(x => new {
                x.UserId,
                x.UserName,
                x.From.Year,
                x.From.Month
            })
            .Select(s => new {
                s.Key.UserId,
                s.Key.UserName,
                Month = new DateOnly(s.Key.Year, s.Key.Month, 1),
                Hours = s.Sum(us => (us.To - us.From).Hours)
            })
            .ToList();     
            
        var start = new DateOnly(date.Year, date.Month, 1).AddMonths(1);
        var months = (from r in Enumerable.Range(1,12) select start.AddMonths(-r)).ToList();

        return grouped.GroupBy(g => new { g.UserId, g.UserName })
            .Select(g => {
                var hoursByMonth = g.ToDictionary(x => x.Month, x => x.Hours);

                return new OverviewData {
                    Id = g.Key.UserId,
                    Name = g.Key.UserName!,
                    Confirmed = months.Select(m => hoursByMonth.TryGetValue(m, out var h) ? h : 0).ToList(),
                    Total = hoursByMonth.Sum(e => e.Value)
                };
            })
            .OrderBy(u => u.Name)
            .ToList();
    }

    public async Task<Dictionary<int, int>> GetRequestedHoursForYear(DateOnly date) {
        DateTime to = new DateTime(date.Year, date.Month, 1).AddMonths(1).AddSeconds(-1);
        DateTime from = new DateTime(date.Year, date.Month, 1).AddMonths(-11);

        var start = new DateOnly(date.Year, date.Month, 1).AddMonths(1);
        var months = (from r in Enumerable.Range(1,12) select start.AddMonths(-r)).ToList();

        var data = await _dbContext.UserShifts
            .Include(e => e.Shift)
            .Where(e => e.User.Active && e.Shift.From >= from && e.Shift.To <= to).ToListAsync();
            
        return data.GroupBy(e => e.UserId)
            .Select(sel => new {
                UserId = sel.Key,
                Hours = sel.Sum(sum => (sum.Shift.To - sum.Shift.From).Hours)
            })
            .ToDictionary(dict => dict.UserId, dict => dict.Hours);

    }
}
