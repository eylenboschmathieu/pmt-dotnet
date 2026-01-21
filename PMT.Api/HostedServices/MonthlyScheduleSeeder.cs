using PMT.Data;
using PMT.Services;

namespace PMT.Api.HostedServices;

public class MonthlyScheduleSeeder(IServiceScopeFactory _scopeFactory, ILogger<MonthlyScheduleSeeder> _logger) : BackgroundService {

    protected override async Task ExecuteAsync(CancellationToken ct) {
        // Optional small delay to let the app finish startup
        await Task.Delay(TimeSpan.FromSeconds(10), ct);

        while (!ct.IsCancellationRequested) {
            try {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                await SchedulingService.EnsureScheduleMonthsAsync(db, monthsAhead: 3, ct);

                _logger.LogInformation("Schedule months ensured successfully.");
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to ensure schedule months.");
            }

            // Add new month when the current month stops accepting requests (Which is currently the 14th, midnight)
            DateTime now = DateTime.UtcNow;
            DateTime then = new(now.Year, now.Month, 15);
            if (now.Day > 14)
                then = then.AddMonths(1);
            TimeSpan ts = then - now;
            Console.WriteLine($"Unlocking next month in {ts.Days} days, {ts.Hours} hours, {ts.Minutes} minutes, and {ts.Seconds} seconds.");
            await Task.Delay(ts, ct);
        }
    }
}
