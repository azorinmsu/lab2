using AccountService;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("Accounts")
        ?? throw new InvalidOperationException("Не задана строка подключения ConnectionStrings:Accounts.");
    return new global::AccountService.AccountService(connectionString);
});

builder.Services.AddHostedService<StartupSqlHostedService>();

var host = builder.Build();
await host.RunAsync();
