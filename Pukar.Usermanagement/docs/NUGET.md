# Publishing Pukar.Usermanagement as NuGet packages

Shared metadata lives in [`Directory.Build.props`](../Directory.Build.props) (`Version`, `Authors`, `RepositoryUrl`, etc.).

## Local pack

```bash
dotnet pack Pukar.Usermanagement.API/Pukar.Usermanagement.API.csproj -c Release -o ../../artifacts
```

The API project sets `IsPackable` so it produces a `.nupkg`. Dependencies resolve via NuGet when consumers use `PackageReference`; for a single package that pulls in Application/Infrastructure/Domain at build time, you may instead publish multiple packages (one per project) with matching versions.

## Feeds (choose one)

| Feed | Typical use |
|------|-------------|
| **nuget.org** | Public libraries |
| **GitHub Packages** | Private repos tied to GitHub; use `dotnet nuget push` with `GITHUB_TOKEN` |
| **Azure Artifacts** | Enterprise Azure DevOps |

Example GitHub Packages push (after configuring `nuget.config` with the feed source):

```bash
dotnet nuget push artifacts/*.nupkg --source "https://nuget.pkg.github.com/OWNER/index.json" --api-key GITHUB_TOKEN
```

## Consumer migration

Replace the host’s `ProjectReference` to `Pukar.Usermanagement.API` with:

```xml
<PackageReference Include="Pukar.Usermanagement.API" Version="1.0.0" />
```

Align the package ID with the `PackageId` in the `.csproj` if you override it (defaults to assembly name).
