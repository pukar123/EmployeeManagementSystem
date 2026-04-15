namespace EMS.Application.Services.Authorization;

public interface IAuthorizationTelemetry
{
    void RecordRoleKeyPathUsed(int roleKeyCount, int allowedMenuCount);

    void RecordLegacyFallbackUsed(int userId, int roleKeyCount);

    void RecordRoleKeyLegacyMismatch(
        int userId,
        int roleKeyMenuCount,
        int legacyMenuCount,
        int roleKeyCount);
}
