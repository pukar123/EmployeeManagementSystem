using EMS.Application.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace EMS.API.Bootstrap;

public sealed class UserManagementHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly UserManagementApiOptions _options;

    public UserManagementHealthCheck(
        IHttpClientFactory httpClientFactory,
        IOptions<UserManagementApiOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            return HealthCheckResult.Unhealthy("UserManagementApi:BaseUrl is not configured.");
        }

        try
        {
            var client = _httpClientFactory.CreateClient(EMS.Infrastructure.Integrations.UserManagement.UserManagementHttpClientNames.Default);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));

            using var response = await client.GetAsync("/health", cts.Token);
            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("User Management is reachable.");
            }

            return HealthCheckResult.Unhealthy(
                $"User Management health endpoint returned {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("User Management is unreachable.", ex);
        }
    }
}
