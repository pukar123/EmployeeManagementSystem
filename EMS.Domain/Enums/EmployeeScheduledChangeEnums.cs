namespace EMS.Domain.Enums;

public enum EmployeeScheduledChangeType
{
    Terminate = 1,
    Archive = 2,
    RestoreRecord = 3,
    ChangeEmploymentStatus = 4,
    TransferDepartment = 5,
    TransferPosition = 6,
    TransferManager = 7,
}

public enum EmployeeScheduledChangeStatus
{
    Pending = 1,
    Processing = 2,
    Applied = 3,
    Cancelled = 4,
    Failed = 5,
}
