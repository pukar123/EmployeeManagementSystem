namespace EMS.Application.DTOs.Employee;

public sealed class EmployeeAccessCapabilitiesResponseModel
{
    public bool View { get; set; }

    public bool Manage { get; set; }

    public bool Access { get; set; }

    public bool Export { get; set; }
}
