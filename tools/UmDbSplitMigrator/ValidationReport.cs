namespace UmDbSplitMigrator;

internal sealed class ValidationCheck
{
    public required string Name { get; init; }

    public required bool Passed { get; init; }

    public required string Detail { get; init; }
}

internal sealed class ValidationReport
{
    private readonly List<ValidationCheck> _checks = [];

    public IReadOnlyList<ValidationCheck> Checks => _checks;

    public bool AllPassed => _checks.All(c => c.Passed);

    public void Add(string name, bool passed, string detail)
    {
        _checks.Add(new ValidationCheck
        {
            Name = name,
            Passed = passed,
            Detail = detail,
        });
    }

    public void WriteToConsole()
    {
        Console.WriteLine();
        Console.WriteLine("Validation report");
        Console.WriteLine(new string('-', 72));
        foreach (var check in _checks)
        {
            var status = check.Passed ? "PASS" : "FAIL";
            Console.WriteLine($"[{status}] {check.Name}: {check.Detail}");
        }

        Console.WriteLine(new string('-', 72));
        Console.WriteLine(AllPassed ? "All checks passed." : "One or more checks failed.");
    }
}
