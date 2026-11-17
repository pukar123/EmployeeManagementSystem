# Dev Org profile: Employee Management System (EMS)

Stack: ASP.NET Core (.NET 9, net9.0), EF Core, SQL Server, Next.js/React, React Query, Zustand, JWT, Redis, MongoDB logging, Docker Compose
Architecture: Onion / Clean Architecture (Domain, Application, Infrastructure, API); frontend in Next.js
Conventions: business rules live in Domain/Application only; controllers stay thin; API responses use DTOs, never entities; async all the way down
Build: dotnet build    Test: dotnet test    Frontend: npm run build
Extra high-risk areas: JWT/auth, roles and permissions, leave balances, pay or rate data, employee personal details, EF Core migrations
Extra medium-risk areas: Redis caching, MongoDB logging, Docker Compose config
Approval changes: scheduled dev-org runs (docs/dev-org/BACKLOG.md) have CEO standing approval for all gates (decided 2026-11-15); interactive /dev-org runs use the standard matrix
Notes: personal portfolio project, never described as commercial work; Australian English
