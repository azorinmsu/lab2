using Npgsql;

namespace AccountService;

public sealed class AccountService
{
    private readonly string _connectionString;

    public AccountService(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Строка подключения не задана.", nameof(connectionString));

        _connectionString = connectionString;
    }

    public async Task AddAsync(double sum, string name, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string selectSql = """
            SELECT id, name, "sum"
            FROM account
            WHERE name = @name
            FOR NO KEY UPDATE
            """;

        await using var select = new NpgsqlCommand(selectSql, connection, transaction);
        select.Parameters.AddWithValue("name", name);

        await using var reader = await select.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException($"Аккаунт с именем '{name}' не найден.");

        var account = new Account
        {
            Id = reader.GetInt64(reader.GetOrdinal("id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Sum = reader.GetDouble(reader.GetOrdinal("sum"))
        };

        await reader.CloseAsync();

        account.Sum += sum;

        const string updateSql = """
            UPDATE account
            SET "sum" = @sum
            WHERE id = @id
            """;

        await using var update = new NpgsqlCommand(updateSql, connection, transaction);
        update.Parameters.AddWithValue("sum", account.Sum);
        update.Parameters.AddWithValue("id", account.Id);
        await update.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
