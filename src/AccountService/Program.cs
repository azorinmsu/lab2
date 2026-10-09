using AccountService;

var builder = Host.CreateApplicationBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Accounts")
    ?? throw new InvalidOperationException("Не задана строка подключения ConnectionStrings:Accounts.");

builder.Services.AddSingleton(new global::AccountService.AccountService(connectionString));
builder.Services.AddSingleton(new ApiAccountService(connectionString));

builder.Services.AddHostedService<StartupSqlHostedService>();

var host = builder.Build();
await host.RunAsync();
