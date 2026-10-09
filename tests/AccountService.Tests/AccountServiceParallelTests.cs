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

        /*  
        Нужно написать тест, который должен запуститься в Testcontainers‑контейнере с PostgreSQL.
        Создать аккаунт с суммой и именем, которые хранятся в переменных AccountName и InitialSum.
        Тест должен в 5 потоках параллельно увеличивать баланс аккаунта на 10 копеек.
        После всех вызовов проверить баланс аккаунта и сопоставить его с ожидаемым значением.
        В сервисе AccountService умышленно добавлен баг, который вызывает гонку между потоками.
        Необходимо замерить общее время работы потоков.
        Нужно предложить решение, как этот баг пофиксить.
        */

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
