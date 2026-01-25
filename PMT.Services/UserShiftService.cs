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

public class UserShiftService(IShiftRepository _shiftRepo, IUserShiftRepository _usershiftRepo, IRoleRepository _roleRepo) {
    private static readonly ShiftTime[] ShiftTimes = [  // We're storing time in UTC, so make sure the shift hours are in UTC time too
        new ShiftTime { From = new TimeOnly(5, 0), Duration = new TimeSpan(3, 0, 0) },
        new ShiftTime { From = new TimeOnly(8, 0), Duration = new TimeSpan(3, 0, 0) },
        new ShiftTime { From = new TimeOnly(11, 0), Duration = new TimeSpan(3, 0, 0) },
        new ShiftTime { From = new TimeOnly(14, 0), Duration = new TimeSpan(4, 0, 0) },
        new ShiftTime { From = new TimeOnly(18, 0), Duration = new TimeSpan(11, 0, 0) }
    ];

    public List<ShiftHours> GetShiftHours() {
        return ShiftTimes.Select(e => new ShiftHours {
            From = e.From,
            To = e.From.Add(e.Duration)
        }).ToList();
    }

    public async Task<IEnumerable<DateOnly>> GetRequestedMonths() =>
        await _usershiftRepo.GetRequestedMonths();

    public async Task<List<UserRequestsDTO>> GetUserRequests(int userId, int year, int month) {
        int daysInMonth = DateTime.DaysInMonth(year, month);
        List<UserRequestsDTO> data = new(daysInMonth);

        Dictionary<TimeOnly, int> hours = ShiftTimes
            .Select(e => e.From)
            .OrderBy(e => e)
            .Select((item, index) => new {
                Key = item,
                Index = index
            }).ToDictionary(e => e.Key, e => e.Index);
        
        for (int day = 1; day <= daysInMonth; day++) {
            DateOnly date = new(year, month, day);

            IEnumerable<UserShift> userShifts = await _usershiftRepo.GetUserRequestsForDay(userId, date);
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
            Shifts = (await _usershiftRepo.GetPlannedShifts(userId, new DateOnly(year, month, 1), new DateOnly(year, month, daysInMonth)))
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
    public async Task<List<MonthsDTO>> GetPlanningMonths() =>
        await _usershiftRepo.GetPlanningMonths();

    public async Task<List<DayPlanningDTO>> GetPlanningForMonth(int year, int month) {
        // See DTO in interface for more information
        int daysInMonth = DateTime.DaysInMonth(year, month);
        List<DayPlanningDTO> data = new(daysInMonth);

        int internId = (await _roleRepo.FindByName("Intern"))?.Id ?? throw new Exception("RoleNotFound");
        
        for (int day = 1; day <= daysInMonth; day++) {
            DayPlanningDTO dayPlanning = new(new DateOnly(year, month, day));

            List<IGrouping<DateTime, UserShift>> userShifts = await _usershiftRepo.GetRequestsForDay(dayPlanning.Date);
            Dictionary<TimeOnly, int> hours = ShiftTimes.Select(e => e.From).OrderBy(e => e).Select((item, index) =>
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

    public async Task<bool> LockMonth(DateOnly date, bool locked) =>
        await _usershiftRepo.LockMonth(date, locked);

    public async Task<bool> UpdateShiftRequest(UpdateRequestDTO dto) => dto.IsRequested ?
        await CreateRequest(dto.UserId, dto.Shift) :
        await DeleteRequest(dto.UserId, dto.Shift);

    private async Task<bool> CreateRequest(int userId, DateTime from) {
        DateTime now = DateTime.UtcNow;
        if (now.Day >= 15)
            now = now.AddMonths(1);
        
        if (from.Month <= now.Month)
            return false;

        Shift? shift = await _shiftRepo.GetAsync(from);
        
        if (shift is null) {
            TimeSpan to = ShiftTimes.Where(e => TimeOnly.FromDateTime(from) == e.From).Select(e => e.Duration).FirstOrDefault();
            shift = new Shift {
                From = DateTime.SpecifyKind(from, DateTimeKind.Utc),
                To = DateTime.SpecifyKind(from.Add(to), DateTimeKind.Utc)
            };
            await _shiftRepo.CreateAsync(shift);
        }

        await _usershiftRepo.CreateAsync(new UserShift {
            UserId = userId,
            ShiftId = shift.Id,
            Planned = false
        });

        return true;
    }

    private async Task<bool> DeleteRequest(int userId, DateTime from) {
        Shift? shift = await _shiftRepo.GetAsync(from);
        if (shift is null)
            return false;

        UserShift? userShift = await _usershiftRepo.GetAsync(userId, shift.Id);
        if (userShift is null)
            return false;

        await _usershiftRepo.DeleteAsync(userShift.Id);

        return true;
    }

    public async Task<bool> UpdateShiftPlanning(int shiftId, bool planned) {
        UserShift? us = await _usershiftRepo.GetAsync(shiftId);
        if (us is null)
            return false;

        us.Planned = planned;
        return await _usershiftRepo.UpdateAsync(us);
    }

    // Starting from {date}, going backwards
    private List<DateOnly> GetLast12Months(DateOnly date) {
        date = new DateOnly(date.Year, date.Month, 1);
        return (from r in Enumerable.Range(0,12) select date.AddMonths(-r)).ToList();
    }

    public async Task<OverviewDTO> GetUserShiftOverview() {
        DateOnly startMonth = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(-1);
        startMonth = new(startMonth.Year, startMonth.Month, 1);

        Dictionary<int, int> totalRequested = await _usershiftRepo.GetRequestedHoursForYear(startMonth);

        OverviewDTO dto = new() {
            Months = GetLast12Months(startMonth),
            Users = await _usershiftRepo.GetOverviewData(startMonth)
        };
        
        foreach (var item in dto.Users) {
            item.Requested = (int)totalRequested.GetValueOrDefault(item.Id, 0);
        }

        return dto;
    }
}
