namespace EMS.Application.Services.Authorization;

public interface IIdentityContext
{
    UserIdentitySnapshot GetCurrent();
}
