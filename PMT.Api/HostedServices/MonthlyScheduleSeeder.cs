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

            // Run once per day
            await Task.Delay(TimeSpan.FromDays(1), ct);
        }
    }
}
