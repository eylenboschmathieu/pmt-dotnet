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
    public IEnumerable<int> Confirmed { get; set; } = [];
    public int Total { get; set; }
    public int Requested { get; set; }
}

public interface IUserShiftRepository : IRepository<UserShift> {
    /// <summary>
    /// Get shift data for this application.
    /// </summary>
    /// <returns>An array containing TimeOnly(Start of shift) and TimeSpan(Shift duration) pairs</returns>
    public ShiftTime[] GetShiftHours();

    /// <summary>
    /// <para>Returns enumeration of months for shift requests.</para>
    /// Only return the months after this month, and this month iff the day is less than the 15th,
    /// as this is the cutoff date to give management the time to make their planning.
    /// </summary>
    /// <returns>An IEnumerable containing DateOnly objects</returns>
    public Task<IEnumerable<DateOnly>> GetRequestedMonths();

    /// <summary>
    /// Returns a list of all user shifts for a given day, grouped by each shift.
    /// </summary>
    /// <param name="date">The date to fetch requests for.</param>
    /// <returns>A list of IGrouping objects containing the shift time as key, and all users' shifts for that shift as value.</returns>
    public Task<List<IGrouping<DateTime, UserShift>>> GetRequestsForDay(DateOnly date);

    /// <summary>
    /// Returns a list of all user shifts for a given user for a given day.
    /// </summary>
    /// <param name="userId">The id of the user</param>
    /// <param name="date">The date of the requests</param>
    /// <returns>An IEnumerable containing UserShift objects for a given day.</returns>
    public Task<IEnumerable<UserShift>> GetUserRequestsForDay(int userId, DateOnly date);

    /// <summary>
    /// Returns a list of shifts that have been planned for a given user.
    /// </summary>
    /// <param name="userId">The id of the user</param>
    /// <param name="from">The start date of the shifts</param>
    /// <param name="to">The end date end of the shifts</param>
    /// <returns>An IEnumerable containing Shift objects.</returns>
    public Task<IEnumerable<Shift>> GetConfirmedShifts(int userId, DateOnly from, DateOnly to);

    /// <summary>
    /// Returns a list of months that can be planned for.
    /// </summary>
    /// <returns>An IEnumerable containing DateOnly(Set to the first day of a month) and bool(Whether or not this month has been locked) pairs.</returns>
    public Task<IEnumerable<MonthsDTO>> GetPlanningMonths();

    /// <summary>
    /// Lock/unlock the planning for a given month.
    /// </summary>
    /// <param name="date">The month to lock/unlock</param>
    /// <param name="locked">Whether or not the month should be locked</param>
    /// <returns>Returns true if locking/unlocking succeeded, false otherwise.</returns>
    public Task<bool> LockMonth(DateOnly date, bool locked);

    /// <summary>
    /// Create a new user shift for a given user at the provided time.
    /// </summary>
    /// <param name="userId">The id of the user</param>
    /// <param name="shift">The start time of the shift</param>
    /// <returns>Returns true if the request was successfully created, false otherwise.</returns>
    public Task<bool> CreateRequest(int userId, DateTime shift);

    /// <summary>
    /// Delete a user shift for a given user at the provided time.
    /// </summary>
    /// <param name="userId">The id of the user</param>
    /// <param name="shift">The start time of the shift</param>
    /// <returns>Returns true of the request was successfully deleted, false otherwise</returns>
    public Task<bool> DeleteRequest(int userId, DateTime shift);

    /// <summary>
    /// Modify whether or not a users' shift was accepted.
    /// </summary>
    /// <param name="shiftId">The id of the user shift</param>
    /// <param name="confirm">Whether or not this user shift is confirmed</param>
    /// <returns>Returns true if the planning was successfully updated, false otherwise.</returns>
    public Task<bool> UpdatePlanningForShift(int shiftId, bool planned);

    /// <summary>
    /// Returns list of all active users, with each users containing how many hours they worked each month, for the last 12 months.
    /// As well as the total worked in the last 12 months, and the percentage of hours requested to hours worked.
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
