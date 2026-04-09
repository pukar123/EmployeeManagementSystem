# Pukar.Shared

Shared **string helpers**, **business-rule exceptions**, and **email normalization** used by:

- `EMS.Domain` / `EMS.Application` / `EMS.API`
- `Pukar.Usermanagement.*`

Single implementation: add behavior here once, reference `Pukar.Shared` from any layer that needs it.

## Contents

| Type | Purpose |
|------|---------|
| `StringHelper` | `NormalizeOptional` / `NormalizeRequired`, email validation, string comparisons |
| `BusinessRuleException` | Domain/application rule violations surfaced to API clients |
| `DuplicateEmailException` | Registration conflict (email already used) |
| `EmailNormalizer` | Upper-invariant email for lookups |

Namespace: `Pukar.Shared`.
