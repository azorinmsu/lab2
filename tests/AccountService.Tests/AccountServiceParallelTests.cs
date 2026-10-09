using System.Diagnostics;
using System.Globalization;
using AccountService;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit.Abstractions;

namespace AccountService.Tests;

public class AccountServiceParallelTests(ITestOutputHelper output)
{
    private const int ThreadCount = 5;
    private const int TotalCalls = 5000;
    private const int BatchSize = 100;
    private const double Sum = 10d;
    private const double InitialSum = 0d;
    private const string AccountName = "main";

    [Fact]
    public async Task FiveThreads_AddTenFiveThousandTimes_PersistExpectedSum()
    {
        var callsPerThread = TotalCalls / ThreadCount;
        Assert.Equal(0, TotalCalls % ThreadCount);
        Assert.Equal(0, callsPerThread % BatchSize);

        await using var postgres = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("accounts")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await postgres.StartAsync();

        var connectionString = postgres.GetConnectionString();
        await DatabaseInitializer.InitializeAsync(connectionString);
        await InsertAccountAsync(connectionString, AccountName, InitialSum);

        var service = new AccountService(connectionString);
        var api = new ApiAccountService(connectionString);

        /*
        Тут нужно сэмулировать аномалию Lost Update между сервисами AccountService и ApiAccountService
        Между select-ом и update-ом паузу в ApiAccountService добавлен timeout, нужно отловить этот баг.
        */

        var actual = await ReadSumAsync(connectionString, AccountName);
        const double finalExpected = 343d;

        Assert.Equal(finalExpected, actual);
    }

    private void WriteLine(string message)
    {
        output.WriteLine(message);
        Console.WriteLine(message);
    }

    private static async Task InsertAccountAsync(string connectionString, string name, double sum)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO account (name, "sum")
            VALUES (@name, @sum)
            """,
            connection);
        command.Parameters.AddWithValue("name", name);
        command.Parameters.AddWithValue("sum", sum);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<double> ReadSumAsync(string connectionString, string name)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """
            SELECT "sum"
            FROM account
            WHERE name = @name
            """,
            connection);
        command.Parameters.AddWithValue("name", name);
        var value = await command.ExecuteScalarAsync();
        return value is double sum
            ? sum
            : throw new InvalidOperationException($"Аккаунт с именем '{name}' не найден.");
    }
}
