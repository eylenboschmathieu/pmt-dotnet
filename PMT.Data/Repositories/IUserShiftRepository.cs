using Humanizer;

using PMT.Data.Entities;

namespace PMT.Data.Repositories;

public class MonthsDTO {
    public DateOnly Date { get; set; }
    public bool Locked { get; set; }
}

public class OverviewData {
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public List<int> Confirmed { get; set; } = [];
    public int Total { get; set; }
    public int Requested { get; set; }
}

public interface IUserShiftRepository : IRepository<UserShift> {
    public ShiftTime[] GetShiftHours();
    public Task<IEnumerable<DateOnly>> GetRequestedMonths();
    public Task<List<IGrouping<DateTime, UserShift>>> GetRequestsForDay(DateOnly date);
    public Task<ICollection<UserShift>> GetUserRequestsForDay(int userId, DateOnly date);
    public Task<ICollection<Shift>> GetConfirmedShifts(int userId, DateOnly from, DateOnly to);
    public Task<ICollection<MonthsDTO>> GetPlanningMonths();
    public Task<bool> LockMonth(DateOnly date, bool locked);
    public Task<bool> CreateRequest(int userId, DateTime shift);
    public Task<bool> DeleteRequest(int userId, DateTime shift);
    public Task<bool> ConfirmPlanningForShift(int shiftId, bool confirm);
    public Task<List<OverviewData>> GetOverviewData(DateOnly date);
    
    /// <summary>
    /// Get the requested hours of all active users for the last 12 months.
    /// </summary>
    /// <param name="date">The date to start from, starting from the month before {date}</param>
    /// <returns>A dictionary where the key/value pairs are UserId/TotalHours, respectively</returns>
    public Task<Dictionary<int, int>> GetRequestedHoursForYear(DateOnly date);
}
