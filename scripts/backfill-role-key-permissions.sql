/*
Idempotent backfill for EMS role-key permissions.

Purpose:
- Copy existing role-id permissions in ems.RolePermissions to role-key permissions in ems.RoleKeyPermissions
- Resolve role keys from um.Roles.NormalizedName
- Safe to run multiple times (MERGE with update/insert behavior)

Preconditions:
1) ems.RoleKeyPermissions exists (run latest EMS migrations)
2) um.Roles exists in the same SQL Server database
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    ;WITH SourcePermissions AS
    (
        SELECT
            r.NormalizedName AS RoleKey,
            rp.MenuId,
            CAST(rp.Allowed AS bit) AS Allowed
        FROM ems.RolePermissions rp
        INNER JOIN um.Roles r ON r.Id = rp.RoleId
        WHERE r.NormalizedName IS NOT NULL
          AND LTRIM(RTRIM(r.NormalizedName)) <> ''
    )
    MERGE ems.RoleKeyPermissions AS target
    USING SourcePermissions AS source
      ON target.RoleKey = source.RoleKey
     AND target.MenuId = source.MenuId
    WHEN MATCHED AND target.Allowed <> source.Allowed THEN
        UPDATE SET target.Allowed = source.Allowed
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (RoleKey, MenuId, Allowed)
        VALUES (source.RoleKey, source.MenuId, source.Allowed);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    THROW;
END CATCH;

-- Validation queries
SELECT COUNT(*) AS LegacyPermissionRows FROM ems.RolePermissions;
SELECT COUNT(*) AS RoleKeyPermissionRows FROM ems.RoleKeyPermissions;

SELECT
    r.NormalizedName AS RoleKey,
    COUNT(*) AS LegacyRows
FROM ems.RolePermissions rp
INNER JOIN um.Roles r ON r.Id = rp.RoleId
GROUP BY r.NormalizedName
ORDER BY r.NormalizedName;

SELECT
    rkp.RoleKey,
    COUNT(*) AS RoleKeyRows
FROM ems.RoleKeyPermissions rkp
GROUP BY rkp.RoleKey
ORDER BY rkp.RoleKey;
