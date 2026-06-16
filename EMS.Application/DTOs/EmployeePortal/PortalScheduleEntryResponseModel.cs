using EMS.Application.DTOs.Shift;
using EMS.Application.DTOs.Task;

namespace EMS.Application.DTOs.EmployeePortal;

public class PortalScheduleEntryResponseModel
{
    public PortalScheduleEntryKind Kind { get; set; }
    public ShiftResponseModel? Shift { get; set; }
    public TaskResponseModel? Task { get; set; }
}
