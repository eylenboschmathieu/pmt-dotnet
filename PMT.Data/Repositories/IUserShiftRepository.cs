using PMT.Data.Entities;

namespace PMT.Data.Repositories;

public class MonthsDTO {
    public DateOnly Date { get; set; }
    public bool Locked { get; set; }
}

public class OverviewData {
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public IEnumerable<int> Confirmed { get; set; } = [];
    public int Total { get; set; }
    public int Requested { get; set; }
}

public interface IUserShiftRepository : IRepository<UserShift> {
    /// <summary>
    /// Get a users' shift.
    /// </summary>
    /// <param name="userId">The user this usershift belongs to</param>
    /// <param name="shiftId">The shift this usershift belongs to</param>
    /// <returns>A UserShift object if found, null otherwise. </returns>
    public Task<UserShift?> GetAsync(int userId, int shiftId);

    /// <summary>
    /// <para>Get a list of months for shift requests.</para>
    /// Only return the months after this month, and this month iff the day is less than the 15th,
    /// as this is the cutoff date to give management the time to make their planning.
    /// </summary>
    /// <returns>An IEnumerable containing DateOnly objects</returns>
    public Task<IEnumerable<DateOnly>> GetRequestedMonths();

    /// <summary>
    /// Get a list of all user shifts for a given day, grouped by each shift.
    /// </summary>
    /// <param name="date">The date to fetch requests for.</param>
    /// <returns>A list of IGrouping objects containing the shift time as key, and all users' shifts for that shift as value.</returns>
    public Task<List<IGrouping<DateTime, UserShift>>> GetRequestsForDay(DateOnly date);

    /// <summary>
    /// Get a list of all user shifts for a given user for a given day.
    /// </summary>
    /// <param name="userId">The id of the user</param>
    /// <param name="date">The date of the requests</param>
    /// <returns>An IEnumerable containing UserShift objects for a given day.</returns>
    public Task<IEnumerable<UserShift>> GetUserRequestsForDay(int userId, DateOnly date);

    /// <summary>
    /// Get a list of shifts that have been planned for a given user.
    /// </summary>
    /// <param name="userId">The id of the user</param>
    /// <param name="from">The start date of the shifts</param>
    /// <param name="to">The end date end of the shifts</param>
    /// <returns>An IEnumerable containing Shift objects.</returns>
    public Task<IEnumerable<Shift>> GetPlannedShifts(int userId, DateOnly from, DateOnly to);

    /// <summary>
    /// Get a list of months that can be planned for.
    /// </summary>
    /// <returns>An IEnumerable containing DateOnly(Set to the first day of a month) and bool(Whether or not this month has been locked) pairs.</returns>
    public Task<List<MonthsDTO>> GetPlanningMonths();

    /// <summary>
    /// Lock/unlock the planning for a given month.
    /// </summary>
    /// <param name="date">The month to lock/unlock</param>
    /// <param name="locked">Whether or not the month should be locked</param>
    /// <returns>Returns true if locking/unlocking succeeded, false otherwise.</returns>
    public Task<bool> LockMonth(DateOnly date, bool locked);

    /// <summary>
    /// Get a list of all active users, with each users containing how many hours they worked each month, for the last 12 months.
    /// As well as the total worked in the last 12 months.
    /// </summary>
    /// <param name="date">The date to start from, going backwards</param>
    /// <returns>A list of objects containing the total confirmed hours, percentage of requested vs confirmed hours,
    /// and an IEnumerable containing confirmed hours for each month.</returns>
    public Task<List<OverviewData>> GetOverviewData(DateOnly date);
    
    /// <summary>
    /// Get the requested hours of all active users for the last 12 months.
    /// </summary>
    /// <param name="date">The date to start from, going backwards</param>
    /// <returns>A dictionary where the key/value pairs are UserId/TotalHours, respectively</returns>
    public Task<Dictionary<int, int>> GetRequestedHoursForYear(DateOnly date);
}
