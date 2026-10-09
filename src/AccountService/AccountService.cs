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

        const string updateSql = """
            UPDATE account
            SET "sum" = sum + @sum
            WHERE name = @name
            """;

        await using var update = new NpgsqlCommand(updateSql, connection, transaction);
        update.Parameters.AddWithValue("sum", sum);
        update.Parameters.AddWithValue("name", name);
        await update.ExecuteNonQueryAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
