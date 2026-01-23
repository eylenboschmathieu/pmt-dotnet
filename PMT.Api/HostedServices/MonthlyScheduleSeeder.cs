using PMT.Data;
using PMT.Services;

namespace PMT.Api.HostedServices;

public sealed class MonthlyScheduleSeeder(IServiceScopeFactory _scopeFactory, ILogger<MonthlyScheduleSeeder> _logger) : BackgroundService {
    private const int CUTOFF_DAY = 15;

    protected override async Task ExecuteAsync(CancellationToken ct) {
        // Optional small delay to let the app finish startup
        await Task.Delay(TimeSpan.FromSeconds(10), ct);

        while (!ct.IsCancellationRequested) {
            try {
                using var scope = _scopeFactory.CreateScope();
                ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                _logger.LogInformation("Scheduled {n} new months.", await SchedulingService.EnsureScheduleMonthsAsync(db, monthsAhead: 3, ct));
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to ensure schedule months.");
            }

            // Add new month when the current month stops accepting requests on the {CUTOFF_DAY}
            DateTime now = DateTime.UtcNow;
            DateTime then = new(now.Year, now.Month, CUTOFF_DAY);
            if (now.Day >= CUTOFF_DAY)
                then = then.AddMonths(1);
            TimeSpan ts = then - now;
            _logger.LogInformation("Unlocking next month in {Days} days, {Hours} hours, {Minutes} minutes, and {Seconds} seconds.",
                ts.Days, ts.Hours, ts.Minutes, ts.Seconds);
            await Task.Delay(ts, ct);
        }
    }
}
