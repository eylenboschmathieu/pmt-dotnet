using Microsoft.EntityFrameworkCore;

using PMT.Data.Entities;

namespace PMT.Data.Repositories;

public class ShiftTime {
    public TimeOnly From { get; set; }
    public TimeSpan Duration { get; set; }
}

public class UserShiftRepository(ApplicationDbContext _dbContext) : IUserShiftRepository {
    public async Task<UserShift?> GetAsync(int id) =>
        await _dbContext.UserShifts.FindAsync(id);
    
    public async Task<UserShift?> GetAsync(int userId, int shiftId) =>
        await _dbContext.UserShifts.FirstOrDefaultAsync(e => e.UserId == userId && e.ShiftId == shiftId);

    public async Task<IEnumerable<UserShift>> GetAllAsync() =>
        await _dbContext.UserShifts.ToListAsync();

    public async Task<UserShift> CreateAsync(UserShift entity) {
        _dbContext.UserShifts.Add(entity);
        await _dbContext.SaveChangesAsync();

        return entity;
    }

    public async Task<bool> UpdateAsync(UserShift entity) {
        bool exists = await _dbContext.UserShifts.AnyAsync(e => e.Id == entity.Id);
        if (!exists)
            return false;

        _dbContext.UserShifts.Update(entity);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id) {
        UserShift? us = await _dbContext.UserShifts.FindAsync(id);

        if (us is null)
            return false;

        _dbContext.UserShifts.Remove(us);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<Shift>> GetPlannedShifts(int userId, DateOnly from, DateOnly to) {
        return await _dbContext.UserShifts
            .Where(e =>
                e.UserId == userId &&
                DateOnly.FromDateTime(e.Shift.From) >= from &&
                DateOnly.FromDateTime(e.Shift.To) <= to &&
                e.Planned)
            .Select(e => e.Shift)
            .OrderBy(e => e.From)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<DateOnly>> GetRequestedMonths() {
        DateOnly now = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(1);
        if (DateTime.UtcNow.Day >= 15)
            now = now.AddMonths(1);

        return await _dbContext.PlanningMonths
            .AsNoTracking()
            .Where(e => !e.Locked && now <= e.Date)
            .Select(e => e.Date)
            .OrderByDescending(e => e)
            .ToListAsync();
    }

    public async Task<List<MonthsDTO>> GetPlanningMonths() {
        DateOnly now = new(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        return await _dbContext.PlanningMonths
            .Where(e => now <= e.Date)
            .Select(e => new MonthsDTO { Date = e.Date, Locked = e.Locked } )
            .OrderByDescending(e => e.Date)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<IGrouping<DateTime, UserShift>>> GetRequestsForDay(DateOnly date) {
        return await _dbContext.UserShifts
            .Where(e => DateOnly.FromDateTime(e.Shift.From) == date)
            .Include(e => e.User).Include(e => e.User.Roles)
            .GroupBy(e => e.Shift.From)
            .AsNoTracking().
            ToListAsync();
    }

    public async Task<IEnumerable<UserShift>> GetUserRequestsForDay(int userId, DateOnly date) {
        return await _dbContext.UserShifts
            .Where(e => e.UserId == userId && DateOnly.FromDateTime(e.Shift.From) == date)
            .Include(e => e.Shift)
            .OrderByDescending(e => e.Shift.From)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> LockMonth(DateOnly date, bool locked) {
        PlanningMonth? pm = await _dbContext.PlanningMonths.FirstOrDefaultAsync(e => e.Date == date);
        if (pm is null)
            return false;

        pm.Locked = locked;
        _dbContext.PlanningMonths.Update(pm);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    public async Task<List<OverviewData>> GetOverviewData(DateOnly date) {
        DateTime to = new DateTime(date.Year, date.Month, 1).AddMonths(1).AddSeconds(-1);
        DateTime from = new DateTime(date.Year, date.Month, 1).AddMonths(-11);

        var query = await _dbContext.UserShifts
            .Where(e => e.Planned)
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
            
        DateOnly start = new DateOnly(date.Year, date.Month, 1).AddMonths(1);
        IEnumerable<DateOnly> months = (from r in Enumerable.Range(1,12) select start.AddMonths(-r)).ToList();

        return grouped.GroupBy(g => new { g.UserId, g.UserName })
            .Select(g => {
                Dictionary<DateOnly, int> hoursByMonth = g.ToDictionary(x => x.Month, x => x.Hours);

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
        
        IEnumerable<UserShift> data = await _dbContext.UserShifts
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
