using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Pukar.Shared;
using Pukar.Usermanagement.Application.Services.Password;
using Pukar.Usermanagement.Domain.DbModels;

namespace EMS.API.IntegrationTests.Harness;

/// <summary>
/// Base class for B05 fixtures that call the anonymous auth endpoints (sign-in, register, refresh,
/// password recovery, invitation acceptance). Each fixture has its own API host, so rate-limit counters
/// start fresh; each test uses its own client IPs (sent with <see cref="TestClientIpStartupFilter.HeaderName"/>)
/// and its own email addresses, so tests in one fixture never share a counter.
/// </summary>
public abstract class AuthEndpointTestFixture : AccessTestFixture
{
    protected const string SignInPath = "/api/auth/login";
    protected const string RegisterPath = "/api/auth/register";
    protected const string RefreshPath = "/api/auth/refresh";
    protected const string ForgotPasswordPath = "/api/auth/forgot-password";
    protected const string ResetPasswordPath = "/api/auth/reset-password";
    protected const string AcceptInvitationPath = "/api/invitations/accept";

    /// <summary>The CORS origin the harness allows (see <see cref="EmsApiFactory"/>).</summary>
    protected const string AllowedOrigin = "http://localhost";

    protected const string RateLimitedCode = "rate_limited";
    protected const string RateLimitedMessage = "Too many attempts. Please wait a moment and try again.";

    private static readonly JsonSerializerOptions ApiJson = new(JsonSerializerDefaults.Web);

    /// <summary>Adds an active (or inactive) User Management user whose password is hashed by the API's own hasher.</summary>
    protected Task SeedUserAsync(string email, string password, bool isActive = true)
        => Factory.ChangeUserManagementDatabaseAsync((db, services) =>
        {
            db.Users.Add(NewUser(email, services.GetRequiredService<IPasswordHasher>().HashPassword(password), isActive));
            return Task.CompletedTask;
        });

    /// <summary>
    /// Adds an inactive user with a pending invitation and returns the raw invitation token.
    /// The stored hash is the upper-case hex SHA-256 of the raw token, as the API stores it.
    /// </summary>
    protected async Task<string> SeedInvitationAsync(string email)
    {
        var rawToken = $"b05-invite-{Guid.NewGuid():N}";
        await Factory.ChangeUserManagementDatabaseAsync((db, services) =>
        {
            var hasher = services.GetRequiredService<IPasswordHasher>();
            var user = NewUser(email, hasher.HashPassword(Guid.NewGuid().ToString("N")), isActive: false);
            var now = DateTime.UtcNow;
            db.AccountInvitations.Add(new AccountInvitation
            {
                User = user,
                ExternalCorrelationId = $"employee:b05:{Guid.NewGuid():N}",
                RecipientEmail = email,
                RecipientDisplayName = "B05 Invitee",
                TokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))),
                ExpiresAtUtc = now.AddDays(7),
                CreatedAtUtc = now,
            });
            return Task.CompletedTask;
        });
        return rawToken;
    }

    /// <summary>The User Management user with this email, or null.</summary>
    protected Task<User?> FindUserAsync(string email)
    {
        var normalized = EmailNormalizer.Normalize(email);
        return Factory.QueryUserManagementDatabaseAsync(db =>
            Task.FromResult(db.Users.SingleOrDefault(u => u.NormalizedEmail == normalized)));
    }

    /// <summary>
    /// POSTs <paramref name="body"/> as JSON (no body when null) anonymously from <paramref name="clientIp"/>,
    /// optionally as a cross-origin request from <paramref name="origin"/>.
    /// </summary>
    protected Task<AuthCallResult> PostAsync(
        string description,
        string path,
        object? body,
        string clientIp,
        string? origin = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path);
        if (body is not null)
            request.Content = JsonContent.Create(body, body.GetType(), options: ApiJson);
        return SendAsync(description, request, clientIp, origin, user: null);
    }

    /// <summary>POSTs a request whose body is empty but marked as JSON.</summary>
    protected Task<AuthCallResult> PostEmptyJsonAsync(string description, string path, string clientIp)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(string.Empty, Encoding.UTF8, "application/json"),
        };
        return SendAsync(description, request, clientIp, origin: null, user: null);
    }

    /// <summary>GETs <paramref name="path"/> as <paramref name="user"/> from <paramref name="clientIp"/>.</summary>
    protected Task<AuthCallResult> GetAsAsync(string description, TestUser user, string path, string clientIp)
        => SendAsync(description, new HttpRequestMessage(HttpMethod.Get, path), clientIp, origin: null, user);

    /// <summary>Asserts the status, with the call and body start in the failure message.</summary>
    protected static void AssertStatus(AuthCallResult result, HttpStatusCode expected)
        => Assert.That(
            result.StatusCode,
            Is.EqualTo(expected),
            $"{result.Description}: expected {(int)expected} {expected}, got {(int)result.StatusCode} {result.StatusCode}. " +
            $"Body starts: {Preview(result.Body)}");

    /// <summary>Asserts anything but 429.</summary>
    protected static void AssertNotRateLimited(AuthCallResult result)
        => Assert.That(
            result.StatusCode,
            Is.Not.EqualTo(HttpStatusCode.TooManyRequests),
            $"{result.Description}: expected not to be rate limited. Body starts: {Preview(result.Body)}");

    /// <summary>
    /// Asserts the shared 429 shape: whole-second <c>Retry-After</c> of at least 1, JSON body with
    /// <c>code = rate_limited</c>, the fixed message and <c>retryAfterSeconds</c> equal to the header.
    /// Returns the parsed body.
    /// </summary>
    protected static JsonElement AssertRateLimited(AuthCallResult result)
    {
        AssertStatus(result, HttpStatusCode.TooManyRequests);

        var header = result.Header("Retry-After");
        Assert.That(header, Is.Not.Null, $"{result.Description}: 429 has no Retry-After header.");
        Assert.That(int.TryParse(header, out var headerSeconds), Is.True, $"{result.Description}: Retry-After '{header}' is not whole seconds.");
        Assert.That(headerSeconds, Is.GreaterThanOrEqualTo(1), $"{result.Description}: Retry-After must be at least 1.");
        Assert.That(result.Header("Content-Type"), Does.StartWith("application/json"), $"{result.Description}: 429 body must be JSON.");

        var json = Parse(result);
        Assert.Multiple(() =>
        {
            Assert.That(json.GetProperty("code").GetString(), Is.EqualTo(RateLimitedCode), $"{result.Description}: code");
            Assert.That(json.GetProperty("message").GetString(), Is.EqualTo(RateLimitedMessage), $"{result.Description}: message");
            Assert.That(json.GetProperty("retryAfterSeconds").GetInt32(), Is.EqualTo(headerSeconds),
                $"{result.Description}: retryAfterSeconds must equal the Retry-After header");
        });
        return json;
    }

    /// <summary>Parses the body as JSON, failing with a readable message if it is not JSON.</summary>
    protected static JsonElement Parse(AuthCallResult result)
    {
        try
        {
            using var document = JsonDocument.Parse(result.Body);
            return document.RootElement.Clone();
        }
        catch (JsonException ex)
        {
            Assert.Fail($"{result.Description}: response is not JSON ({ex.Message}). Body starts: {Preview(result.Body)}");
            throw;
        }
    }

    private async Task<AuthCallResult> SendAsync(
        string description,
        HttpRequestMessage request,
        string clientIp,
        string? origin,
        TestUser? user)
    {
        using (request)
        {
            using var client = user is null ? Factory.CreateAnonymousClient() : Factory.CreateClientAs(user);
            request.Headers.Add(TestClientIpStartupFilter.HeaderName, clientIp);
            if (origin is not null)
                request.Headers.Add("Origin", origin);

            using var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in response.Headers.Concat(response.Content.Headers))
                headers[header.Key] = string.Join(", ", header.Value);

            return new AuthCallResult($"{description} ({request.Method} {request.RequestUri} from {clientIp})", response.StatusCode, body, headers);
        }
    }

    private static User NewUser(string email, string passwordHash, bool isActive)
        => new()
        {
            Email = email,
            NormalizedEmail = EmailNormalizer.Normalize(email),
            PasswordHash = passwordHash,
            UserName = "B05 Test User",
            IsActive = isActive,
            MustChangePassword = false,
            CreatedAtUtc = DateTime.UtcNow,
        };

    private static string Preview(string body)
        => string.IsNullOrEmpty(body) ? "(empty)" : body.Length <= 400 ? body : body[..400] + "...";
}
