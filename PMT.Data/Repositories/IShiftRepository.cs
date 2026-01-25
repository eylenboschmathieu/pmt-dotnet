using PMT.Data.Entities;

namespace PMT.Data.Repositories;

public interface IShiftRepository : IRepository<Shift> {
    /// <summary>
    /// Get a shift.
    /// </summary>
    /// <param name="from">Indicates the start of a shift</param>
    /// <returns>A Shift object, or null if it isn't found.</returns>
    public Task<Shift?> GetAsync(DateTime from);
}
