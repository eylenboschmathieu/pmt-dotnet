using Microsoft.EntityFrameworkCore;

using PMT.Data.Entities;

namespace PMT.Data.Repositories;

public class ShiftRepository(ApplicationDbContext _dbContext) : IShiftRepository {
    
    public async Task<Shift?> GetAsync(int id) =>
        await _dbContext.Shifts.FindAsync(id);
    
    public async Task<Shift?> GetAsync(DateTime from) =>
        await _dbContext.Shifts.FirstOrDefaultAsync(e => e.From == from);

    public async Task<IEnumerable<Shift>> GetAllAsync() =>
        await _dbContext.Shifts.ToListAsync();

    public async Task<Shift> CreateAsync(Shift entity) {
        _dbContext.Shifts.Add(entity);
        await _dbContext.SaveChangesAsync();

        return entity;
    }

    public Task<bool> UpdateAsync(Shift entity) =>
        throw new NotImplementedException();

    public Task<bool> DeleteAsync(int id) =>
        throw new NotImplementedException();
}
