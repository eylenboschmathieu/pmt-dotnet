using Microsoft.EntityFrameworkCore;

using PMT.Data.Repositories;

namespace PMT.Services;

public class LockMonthDTO {
    public DateOnly Date { get; set; }
    public bool Locked { get; set; }
}

public class UpdateShiftPlanningDTO {
    public bool Confirm { get; set; }
    public int ShiftId { get; set; }
}

public class UserRequestsDTO {  // A day of requested shifts
    public DateOnly Date { get; set; } 
    public bool[] Shifts { get; set; } = [];  // List of 5 bools to dictate if a shift was requests, from first to last
}

public class DateTimeSpan {
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}

public class UserConfirmedDTO { // Confirmed shifts
    public List<DateTimeSpan> Shifts { get; set; } = [];
    public double TotalHours { get; set; } = 0;
}

public class PlanningRequestDTO {
    public int Id { get; set; }  // The request Id
    public string Name { get; set; } = null!;  // Name of the user
    public bool IsIntern { get; set; }
}

public class ShiftPlanning {
    public List<PlanningRequestDTO> Volunteered { get; set; } = [];
    public List<PlanningRequestDTO> Confirmed { get; set; } = [];
}

public class DayPlanningDTO {
    public DateOnly Date { get; set; }
    public ShiftPlanning[] Shifts { get; set; } = new ShiftPlanning[5];

    public DayPlanningDTO(DateOnly date) => Date = date;
}

public class UpdateRequestDTO {
    public DateTime Shift { get; set; }
    public bool IsRequested { get; set; }
}

public class ShiftService(IUserShiftRepository _shiftRepo, IRoleRepository _roleRepo) {
    private static readonly Dictionary<TimeOnly, int> SHIFT_HOUR_INDICES = new Dictionary<TimeOnly, int> {
        { new TimeOnly(5, 0), 0 },
        { new TimeOnly(8, 0), 1 },
        { new TimeOnly(11, 0), 2 },
        { new TimeOnly(14, 0), 3 },
        { new TimeOnly(18, 0), 4 },
    };

    public ShiftTime[] GetShiftHours() {
        return _shiftRepo.GetShiftHours();
    }

    public async Task<List<UserRequestsDTO>> GetUserRequests(int userId, int year, int month) {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var data = new List<UserRequestsDTO>(daysInMonth);
        
        for (int day = 1; day <= daysInMonth; day++) {
            var date = new DateOnly(year, month, day);

            var userShifts = await _shiftRepo.GetUserRequestsForDay(userId, date);
            var flags = new bool[5];

            foreach (var userShift in userShifts) {
                flags[SHIFT_HOUR_INDICES[TimeOnly.FromDateTime(userShift.Shift.From)]] = true;
            }
            
            data.Add(new UserRequestsDTO {
                Date = date,
                Shifts = flags
            });
        }

        return data;
    }

    public async Task<UserConfirmedDTO> GetConfirmedShiftsForUser(int userId, int year, int month) {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        UserConfirmedDTO dto = new UserConfirmedDTO {
            Shifts = (await _shiftRepo.GetConfirmedShifts(userId, new DateOnly(year, month, 1), new DateOnly(year, month, daysInMonth)))
                .Select(e => new DateTimeSpan() { From = e.From, To = e.To })
                .ToList()
        };

        foreach (var shift in dto.Shifts) {
            dto.TotalHours += (shift.To - shift.From).TotalHours;
        }

        return dto;
    }

    // Only return months that have yet to happen + the one we're in
    public async Task<List<MonthsDTO>> GetPlanningMonths() {
        IEnumerable<MonthsDTO> months = await _shiftRepo.GetPlanningMonths();
        DateOnly today = DateOnly.FromDateTime(DateTime.Today.AddMonths(-1));
        return months.Where(e => e.Date > today).ToList();
    }

    public async Task<List<DayPlanningDTO>> GetPlanningForMonth(int year, int month) {
        // See DTO in interface for more information
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var data = new List<DayPlanningDTO>(daysInMonth);

        var internId = (await _roleRepo.FindByName("Intern"))?.Id ?? throw new Exception("RoleNotFound");
        
        for (int day = 1; day <= daysInMonth; day++) {
            DayPlanningDTO dayPlanning = new(new DateOnly(year, month, day));

            var userShifts = await _shiftRepo.GetRequestsForDay(dayPlanning.Date);

            foreach (var userShift in userShifts) {
                int index = SHIFT_HOUR_INDICES[TimeOnly.FromDateTime(userShift.Key)];
                
                ShiftPlanning shiftPlanning = new() {
                    Volunteered = userShift.Where(e => !e.Confirmed).Select(e => new PlanningRequestDTO() {
                        Id = e.Id,
                        Name = e.User.Name ?? "NoName",
                        IsIntern = e.User.Roles.Select(e => e.Id).Contains(internId)
                    }).ToList(),
                    Confirmed = userShift.Where(e => e.Confirmed).Select(e => new PlanningRequestDTO() {
                        Id = e.Id,
                        Name = e.User.Name ?? "NoName",
                        IsIntern = e.User.Roles.Select(e => e.Id).Contains(internId)
                    }).ToList()
                };

                dayPlanning.Shifts[index] = shiftPlanning;
            }

            data.Add(dayPlanning);
        }

        return data;
    }

    public async Task<bool> LockMonth(DateOnly date, bool locked) {
        return await _shiftRepo.LockMonth(date, locked);
    }

    public async Task<bool> UpdateShiftRequest(int userId, UpdateRequestDTO dto) {
        if (dto.IsRequested)
            return await _shiftRepo.CreateRequest(userId, dto.Shift);
        else
            return await _shiftRepo.DeleteRequest(userId, dto.Shift);
    }

    public async Task<bool> UpdateShiftPlanning(bool confirm, int shiftId) {
        return await _shiftRepo.ConfirmPlanningForShift(confirm, shiftId);
    }
}
