using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;

using PMT.Data.Entities;
using PMT.Data.Repositories;

namespace PMT.Services;

public class ShiftHours {
    public TimeOnly From { get; set; }
    public TimeOnly To { get; set; }
}

public class LockMonthDTO {  // Lock/unlock a month in management/planning
    public DateOnly Date { get; set; }
    public bool Locked { get; set; }
}

public class UpdateShiftPlanningDTO {
    public bool Planned { get; set; }
    public int ShiftId { get; set; }
}

public class UserRequestsDTO {  // A day of requested shifts
    public DateOnly Date { get; set; } 
    public bool[] Shifts { get; set; } = [];  // Array of 5 bools to dictate if a shift was requests, from first to last
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

public class DayPlanningDTO(DateOnly date) {
    public DateOnly Date { get; set; } = date;
    public ShiftPlanning[] Shifts { get; set; } = new ShiftPlanning[5];
}

public class UpdateRequestDTO {  // Setting/clearing a shift request (user is passed as query param)
    public int UserId { get; set; }
    public DateTime Shift { get; set; }
    public bool IsRequested { get; set; }
}

public class OverviewDTO {
    public List<DateOnly> Months { get; set; } = [];
    public List<OverviewData> Users { get; set; } = [];
}

public class ShiftService(IUserShiftRepository _shiftRepo, IRoleRepository _roleRepo) {
    public List<ShiftHours> GetShiftHours() {
        return _shiftRepo.GetShiftHours().Select(sel => new ShiftHours {
            From = sel.From,
            To = sel.From.Add(sel.Duration)
        }).ToList();
    }

    public async Task<IEnumerable<DateOnly>> GetRequestedMonths() => await _shiftRepo.GetRequestedMonths();

    public async Task<List<UserRequestsDTO>> GetUserRequests(int userId, int year, int month) {
        int daysInMonth = DateTime.DaysInMonth(year, month);
        List<UserRequestsDTO> data = new(daysInMonth);

        Dictionary<TimeOnly, int> hours = _shiftRepo.GetShiftHours()
            .Select(e => e.From)
            .OrderBy(e => e)
            .Select((item, index) => new {
                Key = item,
                Index = index
            }).ToDictionary(e => e.Key, e => e.Index);
        
        for (int day = 1; day <= daysInMonth; day++) {
            DateOnly date = new(year, month, day);

            IEnumerable<UserShift> userShifts = await _shiftRepo.GetUserRequestsForDay(userId, date);
            bool[] flags = new bool[5];

            foreach (UserShift userShift in userShifts) {
                flags[hours[TimeOnly.FromDateTime(userShift.Shift.From)]] = true;
            }
            
            data.Add(new UserRequestsDTO {
                Date = date,
                Shifts = flags
            });
        }

        return data;
    }

    public async Task<UserConfirmedDTO> GetConfirmedShiftsForUser(int userId, int year, int month) {
        int daysInMonth = DateTime.DaysInMonth(year, month);
        UserConfirmedDTO dto = new UserConfirmedDTO {
            Shifts = (await _shiftRepo.GetConfirmedShifts(userId, new DateOnly(year, month, 1), new DateOnly(year, month, daysInMonth)))
                .Select(e => new DateTimeSpan() {
                    From = DateTime.SpecifyKind(e.From, DateTimeKind.Utc),
                    To = DateTime.SpecifyKind(e.To, DateTimeKind.Utc)
                }).ToList()
        };

        foreach (DateTimeSpan shift in dto.Shifts)
            dto.TotalHours += (shift.To - shift.From).TotalHours;

        return dto;
    }

    // Only return months that have yet to happen + the one we're in
    public async Task<List<MonthsDTO>> GetPlanningMonths() {
        IEnumerable<MonthsDTO> months = await _shiftRepo.GetPlanningMonths();
        DateOnly today = DateOnly.FromDateTime(DateTime.Today.ToUniversalTime().AddMonths(-1));
        return months.Where(e => e.Date > today).ToList();
    }

    public async Task<List<DayPlanningDTO>> GetPlanningForMonth(int year, int month) {
        // See DTO in interface for more information
        int daysInMonth = DateTime.DaysInMonth(year, month);
        List<DayPlanningDTO> data = new(daysInMonth);

        int internId = (await _roleRepo.FindByName("Intern"))?.Id ?? throw new Exception("RoleNotFound");
        
        for (int day = 1; day <= daysInMonth; day++) {
            DayPlanningDTO dayPlanning = new(new DateOnly(year, month, day));

            List<IGrouping<DateTime, UserShift>> userShifts = await _shiftRepo.GetRequestsForDay(dayPlanning.Date);
            Dictionary<TimeOnly, int> hours = _shiftRepo.GetShiftHours().Select(e => e.From).OrderBy(e => e).Select((item, index) =>
                new {
                    Key = item,
                    Index = index
                }).ToDictionary(e => e.Key, e => e.Index);

            foreach (IGrouping<DateTime, UserShift> userShift in userShifts) {
                int index = hours[TimeOnly.FromDateTime(userShift.Key)];
                
                ShiftPlanning shiftPlanning = new() {
                    Volunteered = userShift.Where(e => !e.Planned).Select(e => new PlanningRequestDTO() {
                        Id = e.Id,
                        Name = e.User.Name ?? "NoName",
                        IsIntern = e.User.Roles.Select(e => e.Id).Contains(internId)
                    }).ToList(),
                    Confirmed = userShift.Where(e => e.Planned).Select(e => new PlanningRequestDTO() {
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

    public async Task<bool> UpdateShiftRequest(UpdateRequestDTO dto) {
        if (dto.IsRequested)
            return await _shiftRepo.CreateRequest(dto.UserId, dto.Shift);
        else
            return await _shiftRepo.DeleteRequest(dto.UserId, dto.Shift);
    }

    public async Task<bool> UpdateShiftPlanning(int shiftId, bool planned) {
        return await _shiftRepo.UpdatePlanningForShift(shiftId, planned);
    }

    // Starting from {date}
    private List<DateOnly> GetLast12Months(DateOnly date) {
        date = new DateOnly(date.Year, date.Month, 1);
        return (from r in Enumerable.Range(0,12) select date.AddMonths(-r)).ToList();
    }

    public async Task<OverviewDTO> GetUserShiftOverview() {
        DateOnly startMonth = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-1);
        DateOnly first_of_month = new(startMonth.Year, startMonth.Month, 1);

        Dictionary<int, int> totalRequested = await _shiftRepo.GetRequestedHoursForYear(first_of_month);

        OverviewDTO dto = new() {
            Months = GetLast12Months(first_of_month),
            Users = await _shiftRepo.GetOverviewData(first_of_month)
        };
        
        foreach (var item in dto.Users) {
            item.Requested = (int)totalRequested.GetValueOrDefault(item.Id, 0);
        }

        return dto;
    }
}
