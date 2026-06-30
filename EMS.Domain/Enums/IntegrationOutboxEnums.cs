namespace EMS.Domain.Enums;

public enum IntegrationOutboxStatus
{
    Pending = 1,
    Processing = 2,
    Succeeded = 3,
    Failed = 4,
}

public enum IntegrationOutboxMessageType
{
    RevokeEmployeeLinkedIdentity = 1,
    SyncEmployeeRoles = 2,
}
