namespace EMS.Application.Services.Authorization;

public interface IAuthorizationTelemetryReporter
{
    AuthorizationTelemetrySnapshot GetSnapshot();
}
