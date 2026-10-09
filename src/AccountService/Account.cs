namespace AccountService;

public sealed class Account
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public double Sum { get; set; }
}
