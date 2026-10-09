using Npgsql;

namespace AccountService;

public static class DatabaseInitializer
{
    public static Task InitializeAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        var scriptsDirectory = Path.Combine(AppContext.BaseDirectory, "sql");
        return SqlScriptRunner.ExecuteAsync(connectionString, scriptsDirectory, cancellationToken);
    }
}

public static class SqlScriptRunner
{
    public static async Task ExecuteAsync(
        string connectionString,
        string scriptsDirectory,
        CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(scriptsDirectory))
            throw new DirectoryNotFoundException($"Каталог SQL-скриптов не найден: {scriptsDirectory}");

        var scripts = Directory.GetFiles(scriptsDirectory, "*.sql")
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        if (scripts.Length == 0)
            throw new InvalidOperationException($"В каталоге {scriptsDirectory} нет SQL-скриптов.");

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        foreach (var script in scripts)
        {
            var sql = await File.ReadAllTextAsync(script, cancellationToken);
            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
