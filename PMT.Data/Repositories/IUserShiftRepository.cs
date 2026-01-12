using PMT.Data.Entities;

namespace PMT.Data.Repositories;

public class MonthsDTO {
    public DateOnly Date { get; set; }
    public bool Locked { get; set; }
}

public interface IUserShiftRepository : IRepository<UserShift> {
    public ShiftTime[] GetShiftHours();
    public Task<List<IGrouping<DateTime, UserShift>>> GetRequestsForDay(DateOnly date);
    public Task<ICollection<UserShift>> GetUserRequestsForDay(int userId, DateOnly date);
    public Task<ICollection<Shift>> GetConfirmedShifts(int userId, DateOnly from, DateOnly to);
    public Task<ICollection<MonthsDTO>> GetPlanningMonths();
    public Task<ICollection<DateOnly>> GetRequestMonths(int userId);
    public Task<bool> LockMonth(DateOnly date, bool locked);
    public Task<bool> CreateRequest(int userId, DateTime shift);
    public Task<bool> DeleteRequest(int userId, DateTime shift);
    public Task<bool> ConfirmPlanningForShift(bool confirm, int shiftId);
}
