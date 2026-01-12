using PMT.Data;
using PMT.Services;

namespace PMT.Api.HostedServices;

public class MonthlyScheduleSeeder(IServiceScopeFactory _scopeFactory, ILogger<MonthlyScheduleSeeder> _logger) : BackgroundService {

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        // Optional small delay to let the app finish startup
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested) {
            try {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                await SchedulingService.EnsureScheduleMonthsAsync(db, monthsAhead: 3, stoppingToken);

                _logger.LogInformation("Schedule months ensured successfully.");
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to ensure schedule months.");
            }

            // Run once per day
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}
