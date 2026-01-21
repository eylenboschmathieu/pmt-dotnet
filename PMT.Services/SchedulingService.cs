using Microsoft.EntityFrameworkCore;

using PMT.Data;
using PMT.Data.Entities;

namespace PMT.Services;

public class SchedulingService {
    public static async Task EnsureScheduleMonthsAsync(ApplicationDbContext db, int monthsAhead, CancellationToken ct) {
        DateOnly firstOfMonth = new(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        if (DateTime.UtcNow.Day > 15)
            monthsAhead += 1;

        for (int i = 0; i < monthsAhead; i++) {
            DateOnly target = firstOfMonth.AddMonths(i);
            bool exists = await db.PlanningMonths.AnyAsync(e => e.Date == target, ct);

            if (!exists) {
                db.PlanningMonths.Add(new PlanningMonth {
                    Date = target,
                    Locked = false
                });
            }
        }

        Console.WriteLine($"Added {await db.SaveChangesAsync(ct)} months.");
    }

    public static async Task RefreshTokenCleanup(ApplicationDbContext db, CancellationToken ct) {
        DateTime lastMonthFromTodayUTC = DateTime.UtcNow.AddMonths(-1);
        List<RefreshToken> tokens = await db.RefreshTokens.Where(e => e.Revoked < lastMonthFromTodayUTC).ToListAsync(ct);

        db.RefreshTokens.RemoveRange(tokens);
        await db.SaveChangesAsync(ct);
        Console.WriteLine($"Deleted {tokens.Count} expired refresh tokens.");
    }
}
