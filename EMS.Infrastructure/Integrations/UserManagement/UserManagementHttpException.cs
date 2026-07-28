using System.Net;

namespace EMS.Infrastructure.Integrations.UserManagement;

public sealed class UserManagementHttpException : Exception
{
    public UserManagementHttpException(HttpStatusCode statusCode, string message, string? responseBody = null)
        : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }

    public HttpStatusCode StatusCode { get; }

    public string? ResponseBody { get; }

    public bool IsClientError => (int)StatusCode is >= 400 and < 500;

    public bool IsDependencyFailure =>
        StatusCode is HttpStatusCode.Unauthorized
            or HttpStatusCode.Forbidden
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.RequestTimeout
            or >= HttpStatusCode.InternalServerError;
}
