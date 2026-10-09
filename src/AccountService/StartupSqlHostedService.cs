namespace AccountService;

public sealed class StartupSqlHostedService(
    IConfiguration configuration,
    ILogger<StartupSqlHostedService> logger) : IHostedService
{
    private const int MaxAttempts = 30;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("Accounts")
            ?? throw new InvalidOperationException("Не задана строка подключения ConnectionStrings:Accounts.");

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await DatabaseInitializer.InitializeAsync(connectionString, cancellationToken);
                logger.LogInformation("SQL-скрипты выполнены.");
                return;
            }
            catch (Exception exception) when (exception is not OperationCanceledException && attempt < MaxAttempts)
            {
                logger.LogWarning(
                    exception,
                    "Не удалось выполнить SQL-скрипты, попытка {Attempt} из {MaxAttempts}.",
                    attempt,
                    MaxAttempts);
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
