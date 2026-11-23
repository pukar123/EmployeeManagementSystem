using EMS.Application.Services.Authorization;
using EMS.Domain.DbModels;
using EMS.Domain.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace EMS.Application.Services.Attendance;

public sealed class AttendanceAdminAccessService : IAttendanceAdminAccessService
{
    /// <summary>Attendance admin menu key (matches RBAC seed). Not the self-service "attendance" menu.</summary>
    private const string AttendanceAdminMenuKey = "attendance.admin";

    // The admin test for attendance lives here only. B30 will swap it for the single shared admin rule.
    private static readonly HashSet<string> AdminRoleKeys = new(StringComparer.Ordinal)
    {
        "ADMIN",
        "ADMINISTRATOR",
    };

    private readonly IIdentityContext _identityContext;
    private readonly IPermissionEvaluator _permissionEvaluator;
    private readonly IBaseRepository<Menu> _menuRepository;

    private bool? _isAttendanceAdmin;

    public AttendanceAdminAccessService(
        IIdentityContext identityContext,
        IPermissionEvaluator permissionEvaluator,
        IBaseRepository<Menu> menuRepository)
    {
        _identityContext = identityContext;
        _permissionEvaluator = permissionEvaluator;
        _menuRepository = menuRepository;
    }

    public async Task<bool> IsAttendanceAdminAsync(CancellationToken cancellationToken = default)
    {
        // Scoped per request, so the answer is worked out once and reused by every check in the request.
        _isAttendanceAdmin ??= await ResolveAsync(cancellationToken);
        return _isAttendanceAdmin.Value;
    }

    private async Task<bool> ResolveAsync(CancellationToken cancellationToken)
    {
        var roleKeys = _identityContext.GetCurrent().RoleKeys.ToList();
        if (roleKeys.Count == 0)
            return false;

        if (IsAdmin(roleKeys))
            return true;

        var menuId = await _menuRepository.GetQueryable()
            .AsNoTracking()
            .Where(m => m.Key == AttendanceAdminMenuKey)
            .Select(m => m.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (menuId == 0)
            return false;

        return await _permissionEvaluator.IsMenuAllowedAsync(roleKeys, menuId, cancellationToken);
    }

    private static bool IsAdmin(IEnumerable<string> roleKeys)
        => roleKeys.Any(role => AdminRoleKeys.Contains(role));
}
