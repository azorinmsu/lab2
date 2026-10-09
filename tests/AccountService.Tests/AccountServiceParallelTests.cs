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
        var completed = 0;
        var logLock = new object();

        var workers = Enumerable.Range(0, ThreadCount).Select(threadIndex => Task.Run(async () =>
        {
            for (var call = 0; call < callsPerThread; call++)
            {
                await service.AddAsync(Sum, AccountName);
                var done = Interlocked.Increment(ref completed);
                if (done % BatchSize != 0)
                    continue;

                var stored = await ReadSumAsync(connectionString, AccountName);
                var expected = InitialSum + done * Sum;

                lock (logLock)
                {
                    var message = string.Format(
                        CultureInfo.InvariantCulture,
                        "Поток {0}. Пачка из {1} вызовов. Ожидаемое: {2}. В базе: {3}.",
                        threadIndex + 1,
                        BatchSize,
                        expected,
                        stored);
                    output.WriteLine(message);
                    Console.WriteLine(message);
                }
            }
        })).ToArray();

        await Task.WhenAll(workers);

        var actual = await ReadSumAsync(connectionString, AccountName);
        var finalExpected = InitialSum + TotalCalls * Sum;
        output.WriteLine(string.Format(
            CultureInfo.InvariantCulture,
            "Итог. Ожидаемое: {0}. В базе: {1}.",
            finalExpected,
            actual));

        Assert.Equal(finalExpected, actual);
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
