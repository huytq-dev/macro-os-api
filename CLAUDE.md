# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build

# Run the API
dotnet run --project API

# EF Core migrations (run from solution root)
dotnet ef migrations add <MigrationName> --project Infrastructure --startup-project API
dotnet ef database update --project Infrastructure --startup-project API
```

## Architecture

This is a .NET 10 Clean Architecture Web API with five projects:

- **API** — ASP.NET Core entry point. Controllers, middleware, DI extension wiring, action filters.
- **Application** — Application-layer services. Uses Mapster for object mapping. No business logic that touches infrastructure directly.
- **Infrastructure** — EF Core (SQL Server), JWT, Redis, Quartz background jobs, Cloudinary, MailKit. Implements interfaces defined in `Infrastructure.Persistence.Interfaces`.
- **Domain** — Entity models and request/response DTOs. `EntityBase<T>` provides `Id`, `CreatedAt`, `ModifiedAt`. `ISoftDeletable` marks soft-delete entities.
- **Contract** — Shared primitives used across all layers: `ApiResponse<T>`, `AppException` hierarchy, settings POCOs, `RegisterServiceAttribute`, `AutoInjectionExtensions`.

Dependency direction: `API` → `Application` → `Infrastructure` → `Domain` ← `Contract`

## Key Patterns

### Auto DI Registration
Decorate a class with `[RegisterService(typeof(IMyService))]` to have it automatically registered by `AutoInjectionExtensions.AddServicesFromAssembly(...)`. Default lifetime is `Scoped`. Each layer calls `services.AddServicesFromAssembly(assemblyRef)` in its own `ServiceCollectionExtensions`.

### Repository / Unit of Work
`IGenericRepository<T>` provides standard CRUD and query methods for any `EntityBase<Guid>` entity. Access repositories through `IUnitOfWork.Repository<T>()`. Domain-specific repositories go under `IUnitOfWork`'s `#region System Repository` and have their own interface/implementation pairs in `Infrastructure/Persistence/Interfaces|Implementations/Systems/`.

### Response Envelope
All endpoints return `ApiResponse<T>` with `isSuccess`, `statusCode`, `message`, `data`, and `errors` fields.

### Exception Handling
Throw a subclass of `AppException` (e.g. `NotFoundException`, `BadRequestException`, `ConflictException`, `UnauthorizedException`) anywhere in the stack. `GlobalExceptionHandler` catches it and converts it to the `ApiResponse<T>` error format automatically.

### Validation
Register a `FluentValidation.AbstractValidator<TRequest>` for any request type. `FluentValidationFilter` (applied globally) resolves the validator from DI and short-circuits with a 400 before the action runs.

### Background Jobs
Email sending is async via Quartz. Enqueue via `IBackgroundJobService` (`QuartzBackgroundJobService`). The `SendEmailJob` dequeues and sends using MailKit.

### Database
`ApplicationDbContext` is split across two partial files. EF configurations are in `Infrastructure/Data/Configurations/` using `IEntityTypeConfiguration<T>`. `TimestampInterceptor` auto-sets `CreatedAt`/`ModifiedAt`. Migrations run automatically on startup (`ApplyMigrationsAndSeedAsync`); seed data only runs in `Development`.

### Configuration
Typed options are defined in `Contract/Settings/` (`JwtOptions`, `CloudinaryOptions`, `EmailOptions`). Access them via `IAppConfiguration` (implemented by `AppConfiguration`).
