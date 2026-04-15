namespace EMS.Application.Services.Authorization;

public interface IPermissionEvaluator
{
    Task<IReadOnlySet<int>> GetAllowedMenuIdsAsync(
        IReadOnlyList<string> roleKeys,
        CancellationToken cancellationToken = default);

    Task<bool> IsMenuAllowedAsync(
        IReadOnlyList<string> roleKeys,
        int menuId,
        CancellationToken cancellationToken = default);
}
