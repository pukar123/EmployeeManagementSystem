namespace EMS.Application.Services.Authorization;

public interface IAuditContextAccessor
{
    UserIdentitySnapshot GetCurrentUser();

    string? GetCorrelationId();
}
