using Pukar.Usermanagement.Contracts.ServiceAuth;

namespace Pukar.Usermanagement.Application.Services.ServiceAuth;

public interface IServiceAuthService
{
    Task<ServiceTokenResponseModel> IssueTokenAsync(ServiceTokenRequestModel request, CancellationToken cancellationToken = default);
}
