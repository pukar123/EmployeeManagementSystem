# EMS architecture (overview)

This document is the **EMS-focused** map: diagrams and how requests flow through this solution today. The **canonical guide** for layering, naming, `Pukar.Shared`, `Pukar.Usermanagement`, frontend, Git, and checklists—reusable across **all** projects—is [ARCHITECTURE_AND_PATTERNS.md](ARCHITECTURE_AND_PATTERNS.md). For business goals, personas, capability scope, and roadmap context, see [business-perspective.md](business-perspective.md). Expand this file when you add authentication, validation pipelines, or deployment-specific concerns that are specific to EMS.

## Layering

```mermaid
flowchart TB
  subgraph api [EMS.API]
    Controllers[Controllers]
  end
  subgraph app [EMS.Application]
    DTOs[DTOs per area Employee Org Dept Location]
    Services[Services per entity]
    Mapping[Mapping helpers]
  end
  subgraph infra [EMS.Infrastructure]
    Repo[BaseRepository of T]
  end
  subgraph domain [EMS.Domain]
    DbCtx[AppDbContext]
    Entities[DbModels]
    RepoIf[IBaseRepository of T]
  end
  Controllers --> Services
  Services --> Mapping
  Services --> RepoIf
  Repo -.-> RepoIf
  Repo --> DbCtx
  DbCtx --> Entities
```

## Responsibility split

- **Controllers** accept HTTP requests and return DTOs; they do not reference EF Core or `DbContext` directly.
- **Application services** orchestrate use cases: load or create entities via `IBaseRepository<T>`, map to/from DTOs, call `SaveChangesAsync` through the repository.
- **Mappers** (static helpers in `EMS.Application/Mapping`) keep mapping logic in one place per aggregate.
- **Domain** owns persistence model, EF configuration, and migrations; **Infrastructure** implements `IBaseRepository<T>` using `AppDbContext`.

## DTO conventions

- DTOs live under `EMS.Application/DTOs` grouped by business area (e.g. `Employee/`, `Organization/`, `Department/`, `Location/`). Each folder holds that area’s create/update request models and response model(s); add more types to the same folder when new tables relate to that area.
- **Create** and **Update** use separate request types (`Create*RequestModel`, `Update*RequestModel`).
- **Read** uses `*ResponseModel` for both detail and list (list is the same shape repeated).
- Namespaces match folders: `EMS.Application.DTOs.Employee`, `EMS.Application.DTOs.Organization`, etc.
- DTOs contain scalars and enums only (no navigation properties); foreign keys are expressed as `int` / nullable `int` as appropriate.

## Dependency injection

Registrations live in `EMS.API/Program.cs`: open-generic `IBaseRepository<>` → `BaseRepository<>`, plus scoped application services per entity area.
