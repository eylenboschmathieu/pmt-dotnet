using PMT.Data;
using PMT.Services;

namespace PMT.Api.HostedServices;

public class WeeklyRefreshTokenCleanup(IServiceScopeFactory _scopeFactory, ILogger<WeeklyRefreshTokenCleanup> _logger) : BackgroundService {

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        // Optional small delay to let the app finish startup
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested) {
            try {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                await SchedulingService.RefreshTokenCleanup(db, stoppingToken);  // Clean up refresh tokens that expired more than a month ago

                _logger.LogInformation("Database refresh tokens cleaned up.");
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to clean up database refresh tokens.");
            }

            // Run once per week
            await Task.Delay(TimeSpan.FromDays(7), stoppingToken);
        }
    }
}