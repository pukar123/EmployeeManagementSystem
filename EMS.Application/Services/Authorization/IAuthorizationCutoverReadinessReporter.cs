namespace EMS.Application.Services.Authorization;

public interface IAuthorizationCutoverReadinessReporter
{
    AuthorizationCutoverReadinessSnapshot GetReadinessSnapshot();
}
