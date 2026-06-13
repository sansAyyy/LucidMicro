# Backend CRUD Module Template

This template generates a backend CRUD feature that follows the LucidMicro service structure.

The first version intentionally keeps the generated entity small: it creates an entity with `Name`, `IsActive`, audit fields, soft delete, pagination, validation, specifications, EF Core configuration, and an API controller.

Use `scripts/new-crud.ps1` to render the template into an existing service.

```powershell
.\scripts\new-crud.ps1 `
  -ServiceName Identity `
  -FeatureName Roles `
  -EntityName Role `
  -Route api/roles `
  -TableName roles
```

Generated files are a starting point. Add service-specific fields and business rules after generation.

After generation, register the generated application service in the service Application project's dependency injection entry:

```csharp
services.AddScoped<I{FeatureName}ApplicationService, {FeatureName}ApplicationService>();
```

If the service DbContext exposes explicit `DbSet<TEntity>` properties, add one for the generated entity. The generic repository can still use `Set<TEntity>()` without an explicit `DbSet`.
