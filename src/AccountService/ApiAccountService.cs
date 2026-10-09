using Npgsql;

namespace AccountService;

public sealed class ApiAccountService
{
    private readonly string _connectionString;

    public ApiAccountService(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Строка подключения не задана.", nameof(connectionString));

        _connectionString = connectionString;
    }

    public async Task ResetAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Имя аккаунта не задано.", nameof(name));

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string selectSql = """
            SELECT id, "sum"
            FROM account
            WHERE name = @name
            """;

        await using var select = new NpgsqlCommand(selectSql, connection, transaction);
        select.Parameters.AddWithValue("name", name);

        await using var reader = await select.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException($"Аккаунт с именем '{name}' не найден.");

        var id = reader.GetInt64(reader.GetOrdinal("id"));
        var sum = reader.GetDouble(reader.GetOrdinal("sum"));
        await reader.CloseAsync();

        var updated = sum == 0d ? 333d : 0d;
        if (sum == 0d)
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);

        const string updateSql = """
            UPDATE account
            SET "sum" = @sum
            WHERE id = @id
            """;

        await using var update = new NpgsqlCommand(updateSql, connection, transaction);
        update.Parameters.AddWithValue("sum", updated);
        update.Parameters.AddWithValue("id", id);
        await update.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
