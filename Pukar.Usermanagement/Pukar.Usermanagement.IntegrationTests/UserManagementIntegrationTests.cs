using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pukar.Usermanagement.Contracts.Auth;
using Pukar.Usermanagement.Contracts.Invitations;
using Pukar.Usermanagement.Contracts.Roles;
using Pukar.Usermanagement.Contracts.ServiceAuth;
using Pukar.Usermanagement.Contracts.Users;
using Pukar.Usermanagement.Domain.Database;
using Pukar.Usermanagement.Domain.DbModels;
using Pukar.Usermanagement.Application.Services.Password;

namespace Pukar.Usermanagement.IntegrationTests;

[TestFixture]
public sealed class UserManagementIntegrationTests
{
    private UmWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new UmWebApplicationFactory();
        _client = _factory.CreateClient();
        SeedRolesAndServiceClient();
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task LoginAndRefresh_RotatesRefreshToken()
    {
        await RegisterUserAsync("user@example.com", "Password123!", "Test User");

        var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestModel
        {
            Email = "user@example.com",
            Password = "Password123!",
        });
        Assert.That(login.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var auth = await login.Content.ReadFromJsonAsync<AuthResponseModel>();
        Assert.That(auth, Is.Not.Null);

        var refresh = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequestModel
        {
            RefreshToken = auth!.RefreshToken,
        });
        Assert.That(refresh.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var refreshed = await refresh.Content.ReadFromJsonAsync<AuthResponseModel>();
        Assert.That(refreshed!.RefreshToken, Is.Not.EqualTo(auth.RefreshToken));
    }

    [Test]
    public async Task RefreshReplay_FailsOnSecondUse()
    {
        await RegisterUserAsync("replay@example.com", "Password123!", "Replay User");
        var auth = await LoginAsync("replay@example.com", "Password123!");

        var first = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequestModel { RefreshToken = auth.RefreshToken });
        Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var second = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequestModel { RefreshToken = auth.RefreshToken });
        Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task ServiceToken_InvalidSecret_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/internal/v1/service-token", new ServiceTokenRequestModel
        {
            ClientId = "ems",
            ClientSecret = "wrong-secret",
        });
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task ServiceToken_ValidClient_ReturnsScopedToken()
    {
        var token = await GetServiceTokenAsync();
        Assert.That(token.Scopes, Does.Contain(ServiceScopes.UsersRead));
    }

    [Test]
    public async Task InternalLookup_ByEmail_ReturnsUser()
    {
        await RegisterUserAsync("lookup@example.com", "Password123!", "Lookup User");
        var serviceToken = (await GetServiceTokenAsync()).AccessToken;

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/internal/v1/users/by-email?email=lookup@example.com");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceToken);
        var response = await _client.SendAsync(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task Invitation_CreateIsIdempotent_ByCorrelation()
    {
        var serviceToken = (await GetServiceTokenAsync()).AccessToken;
        var body = new CreateInvitationRequestModel
        {
            ExternalCorrelationId = "employee:99",
            RecipientEmail = "invite@example.com",
            RecipientDisplayName = "Invite User",
        };

        var first = await PostInternalAsync<InvitationResponseModel>("/api/internal/v1/invitations", body, serviceToken, "invite:employee:99");
        var second = await PostInternalAsync<InvitationResponseModel>("/api/internal/v1/invitations", body, serviceToken, "invite:employee:99-retry");
        Assert.That(second.Id, Is.EqualTo(first.Id));
    }

    [Test]
    public async Task Invitation_Accept_ActivatesUser()
    {
        var serviceToken = (await GetServiceTokenAsync()).AccessToken;
        var created = await PostInternalAsync<InvitationResponseModel>(
            "/api/internal/v1/invitations",
            new CreateInvitationRequestModel
            {
                ExternalCorrelationId = "employee:100",
                RecipientEmail = "accept@example.com",
                RecipientDisplayName = "Accept User",
            },
            serviceToken,
            "invite:employee:100");

        var rawToken = await ExtractInvitationTokenFromDatabaseAsync(created.Id);
        var accept = await _client.PostAsJsonAsync("/api/invitations/accept", new AcceptInvitationRequestModel
        {
            Token = rawToken,
            NewPassword = "NewPassword123!@",
        });
        Assert.That(accept.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        using var lookup = new HttpRequestMessage(HttpMethod.Get, "/api/internal/v1/users/by-email?email=accept@example.com");
        lookup.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceToken);
        var userResponse = await _client.SendAsync(lookup);
        var user = await userResponse.Content.ReadFromJsonAsync<UserSummaryResponseModel>();
        Assert.That(user!.IsActive, Is.True);
    }

    [Test]
    public async Task RevokeSessions_BlocksRefresh()
    {
        await RegisterUserAsync("sessions@example.com", "Password123!", "Sessions User");
        var auth = await LoginAsync("sessions@example.com", "Password123!");
        var serviceToken = (await GetServiceTokenAsync()).AccessToken;

        using var revoke = new HttpRequestMessage(HttpMethod.Post, $"/api/internal/v1/users/{auth.User.Id}/revoke-sessions");
        revoke.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceToken);
        revoke.Headers.Add("Idempotency-Key", $"revoke-sessions:{auth.User.Id}");
        var revokeResponse = await _client.SendAsync(revoke);
        Assert.That(revokeResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var refresh = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequestModel { RefreshToken = auth.RefreshToken });
        Assert.That(refresh.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task InternalEndpoint_UserToken_IsRejected()
    {
        await RegisterUserAsync("internal-block@example.com", "Password123!@#", "Internal Block");
        var auth = await LoginAsync("internal-block@example.com", "Password123!@#");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/internal/v1/roles/metadata");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var response = await _client.SendAsync(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task BatchUserLookup_DoesNotRequireIdempotencyKey()
    {
        var serviceToken = (await GetServiceTokenAsync()).AccessToken;
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/internal/v1/users/lookup")
        {
            Content = JsonContent.Create(new BatchUserLookupRequestModel { UserIds = [] }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceToken);

        var response = await _client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task InternalMutation_IdempotencyKey_ReplayReturnsSameResult()
    {
        var serviceToken = (await GetServiceTokenAsync()).AccessToken;
        var body = new CreateInvitationRequestModel
        {
            ExternalCorrelationId = "employee:idempotent:1",
            RecipientEmail = "idempotent@example.com",
            RecipientDisplayName = "Idempotent User",
        };
        const string key = "test-idempotency-replay-key";

        var first = await PostInternalAsync<InvitationResponseModel>("/api/internal/v1/invitations", body, serviceToken, key);
        var second = await PostInternalAsync<InvitationResponseModel>("/api/internal/v1/invitations", body, serviceToken, key);

        Assert.That(second.Id, Is.EqualTo(first.Id));
    }

    [Test]
    public async Task InternalMutation_IdempotencyKey_ConflictingPayloadReturns409()
    {
        var serviceToken = (await GetServiceTokenAsync()).AccessToken;
        const string key = "test-idempotency-conflict-key";

        await PostInternalAsync<InvitationResponseModel>(
            "/api/internal/v1/invitations",
            new CreateInvitationRequestModel
            {
                ExternalCorrelationId = "employee:idempotent:2",
                RecipientEmail = "conflict-a@example.com",
            },
            serviceToken,
            key);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/internal/v1/invitations")
        {
            Content = JsonContent.Create(new CreateInvitationRequestModel
            {
                ExternalCorrelationId = "employee:idempotent:3",
                RecipientEmail = "conflict-b@example.com",
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceToken);
        request.Headers.Add("Idempotency-Key", key);
        var response = await _client.SendAsync(request);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task Jwks_Endpoint_ReturnsNotFound_WhenRsaDisabledInTesting()
    {
        var response = await _client.GetAsync("/.well-known/jwks.json");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task B05_UM_01_Register_IsOffByDefault_Returns404AndCreatesNoUser()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequestModel
        {
            Email = "self-register@example.com",
            Password = "Password123!@",
            UserName = "Self Register",
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.That(body.RootElement.GetProperty("code").GetString(), Is.EqualTo("registration_disabled"));

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();
        Assert.That(db.Users.Any(u => u.NormalizedEmail == "SELF-REGISTER@EXAMPLE.COM"), Is.False);
    }

    [Test]
    public async Task B05_UM_02_Register_WhenTurnedOn_ReturnsTokens()
    {
        RestartWithSettings(new Dictionary<string, string?>
        {
            ["Registration:AllowSelfRegistration"] = "true",
        });

        var response = await _client.PostAsJsonAsync("/api/auth/register", new RegisterRequestModel
        {
            Email = "allowed@example.com",
            Password = "Password123!@",
            UserName = "Allowed User",
        });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseModel>();
        Assert.That(auth!.AccessToken, Is.Not.Empty);
        Assert.That(auth.RefreshToken, Is.Not.Empty);
    }

    [Test]
    public async Task B05_UM_03_Login_PerIpLimit_ThirdAttemptReturns429WithRetryAfter()
    {
        RestartWithSettings(new Dictionary<string, string?>
        {
            ["AuthRateLimiting:SignInPerIp:PermitLimit"] = "2",
            ["AuthRateLimiting:SignInPerIp:WindowSeconds"] = "300",
        });
        var body = new LoginRequestModel { Email = "nobody@example.com", Password = "Wrong-password-1" };

        var first = await _client.PostAsJsonAsync("/api/auth/login", body);
        var second = await _client.PostAsJsonAsync("/api/auth/login", body);
        var third = await _client.PostAsJsonAsync("/api/auth/login", body);

        Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(third.StatusCode, Is.EqualTo((HttpStatusCode)429));
        Assert.That(third.Headers.RetryAfter?.Delta, Is.Not.Null, "429 must carry Retry-After in seconds.");
        Assert.That(third.Headers.RetryAfter!.Delta!.Value.TotalSeconds, Is.GreaterThanOrEqualTo(1));
        using var json = JsonDocument.Parse(await third.Content.ReadAsStringAsync());
        Assert.That(json.RootElement.GetProperty("code").GetString(), Is.EqualTo("rate_limited"));
    }

    /// <summary>
    /// Replaces this test's host with one that has extra settings (one host at a time, so the static
    /// Serilog logger is never set up by two hosts at once). TearDown disposes the replacement.
    /// </summary>
    private void RestartWithSettings(IReadOnlyDictionary<string, string?> settings)
    {
        _client.Dispose();
        _factory.Dispose();
        _factory = new UmWebApplicationFactory(settings);
        _client = _factory.CreateClient();
        SeedRolesAndServiceClient();
    }

    private void SeedRolesAndServiceClient()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();
        db.Database.EnsureCreated();

        if (!db.Roles.Any())
        {
            db.Roles.Add(new Role
            {
                Name = WellKnownRoles.Admin,
                NormalizedName = WellKnownRoles.AdminNormalizedName,
                IsSystem = true,
            });
            db.SaveChanges();
        }

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        if (!db.ServiceClients.Any())
        {
            db.ServiceClients.Add(new ServiceClient
            {
                ClientId = "ems",
                Name = "EMS Test",
                SecretHash = hasher.HashPassword("test-secret"),
                AllowedScopes = string.Join(' ', ServiceScopes.UsersRead, ServiceScopes.UsersManage, ServiceScopes.RolesRead, ServiceScopes.RolesManage, ServiceScopes.InvitationsManage),
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
            });
            db.SaveChanges();
        }
    }

    /// <summary>
    /// Creates an active user directly in the database. Self-registration is off by default (B05),
    /// so tests no longer depend on <c>/api/auth/register</c> to get a user.
    /// </summary>
    private async Task RegisterUserAsync(string email, string password, string userName)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        db.Users.Add(new User
        {
            Email = email,
            NormalizedEmail = Pukar.Shared.EmailNormalizer.Normalize(email),
            PasswordHash = hasher.HashPassword(password),
            UserName = userName,
            IsActive = true,
            MustChangePassword = false,
            CreatedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    private async Task<AuthResponseModel> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestModel { Email = email, Password = password });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponseModel>())!;
    }

    private async Task<ServiceTokenResponseModel> GetServiceTokenAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/internal/v1/service-token", new ServiceTokenRequestModel
        {
            ClientId = "ems",
            ClientSecret = "test-secret",
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ServiceTokenResponseModel>())!;
    }

    private async Task<T> PostInternalAsync<T>(string url, object body, string serviceToken, string? idempotencyKey = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceToken);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    private async Task<string> ExtractInvitationTokenFromDatabaseAsync(int invitationId)
    {
        // Integration tests cannot read the raw token from the API; re-issue by resending and capturing from a test hook.
        // For accept flow we set password via internal path: use a known token by creating invitation in-process.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UserManagementDbContext>();
        var invitation = db.AccountInvitations.Single(i => i.Id == invitationId);

        const string rawToken = "integration-test-token-value-1234567890";
        invitation.TokenHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken)));
        db.SaveChanges();
        return rawToken;
    }
}
