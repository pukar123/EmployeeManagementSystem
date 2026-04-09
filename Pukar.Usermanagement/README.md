# Pukar.Usermanagement

Layered JWT authentication with refresh-token rotation, BCrypt passwords, and SQL Server persistence (schema `um`). Intended as a reusable module for multiple applications.

**Cursor / VS Code:** clone this repository, open the repo folder (or open `Pukar.Usermanagement.sln` directly). `.vscode/settings.json` sets `dotnet.defaultSolution` so the solution is picked up automatically.

## Projects

| Project | Role |
|---------|------|
| `Pukar.Shared` | Shared `StringHelper`, `BusinessRuleException`, `DuplicateEmailException`, `EmailNormalizer` (also referenced by EMS) |
| `Pukar.Usermanagement.Domain` | Entities, `UserManagementDbContext`, migrations, repository interfaces |
| `Pukar.Usermanagement.Application` | DTOs, `AuthService`, options |
| `Pukar.Usermanagement.Infrastructure` | EF repositories, JWT signing, BCrypt, `AddPukarUserManagement` |
| `Pukar.Usermanagement.API` | `AuthController`, `AddPukarUserManagementApi`, JWT bearer registration |

## Build

```bash
dotnet build Pukar.Usermanagement.sln
```

## Configuration (host app)

Connection string: `UserManagement` or fallback `DefaultConnection`. JWT section name: `Jwt`.

```json
"ConnectionStrings": {
  "UserManagement": "Server=...;Database=...;Trusted_Connection=True;TrustServerCertificate=True"
},
"Jwt": {
  "Issuer": "your-app",
  "Audience": "your-app",
  "SigningKey": "REPLACE_WITH_AT_LEAST_32_CHARACTERS!!",
  "AccessTokenExpirationMinutes": 15,
  "RefreshTokenExpirationDays": 7
}
```

## Database

```bash
dotnet ef database update --project Pukar.Usermanagement.Domain --startup-project Pukar.Usermanagement.Infrastructure --context UserManagementDbContext
```

If the design-time factory uses a different SQL instance than your host (see `UserManagementDbContextFactory`), pass the **same** connection string the app uses:

```bash
dotnet ef database update --project Pukar.Usermanagement.Domain --startup-project Pukar.Usermanagement.Infrastructure --context UserManagementDbContext --connection "Server=...;Database=...;"
```

## Hosting integration

```csharp
builder.Services.AddControllers().AddPukarUserManagementControllers();
builder.Services.AddPukarUserManagementApi(builder.Configuration);

app.UseAuthentication();
app.UseAuthorization();
```

## Repository layout (monorepo consumers)

If this code is consumed from another solution via **git submodule** or **ProjectReference**, see your host application’s documentation for publish/submodule steps.

## NuGet (optional)

Packaging metadata is in `Directory.Build.props`. To produce packages locally:

```bash
dotnet pack Pukar.Usermanagement.API/Pukar.Usermanagement.API.csproj -c Release -o ../../artifacts
```

Point consumers at a feed (GitHub Packages, Azure Artifacts, nuget.org) when you are ready to switch from `ProjectReference` to `PackageReference`.
