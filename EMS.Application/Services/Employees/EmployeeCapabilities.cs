namespace EMS.Application.Services.Employees;

public static class EmployeeCapabilities
{
    public const string View = "employees.view";
    public const string Manage = "employees.manage";
    public const string Access = "employees.access";
    public const string Export = "employees.export";

    public static readonly IReadOnlyList<string> All =
    [
        View,
        Manage,
        Access,
        Export,
    ];

    /// <summary>Capabilities that also grant <see cref="View"/>.</summary>
    public static readonly IReadOnlyList<string> ViewImpliedBy =
    [
        Manage,
        Access,
        Export,
    ];
}
