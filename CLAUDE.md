# CLAUDE.md

## Commands
```bash
dotnet build
dotnet run --project API
dotnet ef migrations add <Name> --project Infrastructure --startup-project API
dotnet ef database update --project Infrastructure --startup-project API
```

## Architecture
5 projects: `API` → `Application` → `Infrastructure` → `Domain` ← `Contract`

| Project | Role |
|---------|------|
| API | Controllers, middleware, filters |
| Application | CQRS handlers (MediatR), validators |
| Infrastructure | EF Core, JWT, Redis, Quartz, Cloudinary |
| Domain | Entities, enums. `EntityBase<Guid>` → Id/CreatedAt/ModifiedAt. `ISoftDeletable` → IsDeleted/DeletedAt |
| Contract | `ApiResponse<T>`, `AppException` hierarchy, settings, `RegisterServiceAttribute` |

**Flat namespace** — mọi file dùng namespace khớp tên project, không sub-namespace. `Domain/Entities/User/User.cs` → `namespace Domain;`

## Patterns

**DI** — Dùng `[RegisterService(typeof(IService))]`. Không register thủ công.

**Repository** — Truy cập qua `IUnitOfWork.Repository<T>()`. Application dùng `FindAsync`/`AnyAsync` từ `IGenericRepository<T>`, không gọi EF Core trực tiếp.

**Response** — Mọi endpoint trả `ApiResponse<T>`.

**UserId trong Controller** — Luôn lấy từ JWT qua `User.GetUserId()`, không nhận UserId từ request body. Các command có `UserId` phải inject từ controller:
```csharp
var userId = User.GetUserId();
await _sender.Send(new MyCommand(userId, ...), ct);
```

**Exception** — Throw subclass của `AppException` (`NotFoundException`, `BadRequestException`, `ConflictException`, `UnauthorizedException`). `GlobalExceptionHandler` tự convert.

**Background jobs** — Email qua `IBackgroundJobService.EnqueueEmailAsync(EmailContent)`.

**Config** — Typed options trong `Contract/Settings/`. Truy cập qua `IAppConfiguration`.

**Mapster** — Bắt buộc dùng `IMapper` (inject) khi:
- Map Entity → Response/DTO (kể cả 1 field)
- Map có flatten từ nested object (e.g. `User.Profile.FirstName` → `Response.FirstName`)
- Map có custom convert (e.g. enum → string)
- Cùng một mapping dùng ở nhiều hơn 1 chỗ

Không dùng Mapster khi: tạo Entity từ Command có business logic (hash password, generate ID, set default role...).

Tạo mapping trong `Application/Mappings/<Feature>MappingConfig.cs` implement `IRegister`. Không dùng `.Adapt<T>()` inline — luôn inject `IMapper`.

```csharp
// MappingConfig
config.NewConfig<User, GetProfileResponse>()
    .Map(dest => dest.FullName, src => $"{src.Profile.FirstName} {src.Profile.LastName}");

// Handler
public sealed class MyHandler(IUnitOfWork _uow, IMapper _mapper) ...
return _mapper.Map<GetProfileResponse>(user);
```

**Database** — `ApplicationDbContext` split 2 partial files. EF config dùng `IEntityTypeConfiguration<T>`. `TimestampInterceptor` tự set timestamps. Migration tự chạy khi startup.

## C# Style Rules

- **Primary constructor** cho mọi class có DI:
  ```csharp
  public sealed class MyHandler(IUnitOfWork _uow) : IRequestHandler<MyCommand, Unit> { }
  ```
- **`sealed`** cho handler/service không có kế thừa.
- **`sealed record`** cho Command/Query/Response.

## Messages — Bắt buộc, không hardcode string

| Class | Dùng ở đâu |
|-------|-----------|
| `ValidationMessages` | Validator `.WithMessage(...)` |
| `ExceptionMessages` | Handler khi throw exception |
| `BusinessMessages` | Handler khi return success message |

```csharp
// Validator
RuleFor(x => x.Email).NotEmpty().WithMessage(ValidationMessages.Required)
                      .EmailAddress().WithMessage(ValidationMessages.InvalidEmail);

// Handler
throw new NotFoundException(ExceptionMessages.NotFound("User", id));
throw new ConflictException(ExceptionMessages.AlreadyExists("Email", value));
return BusinessMessages.UpdatedSuccessfully("User");
```
Location: `Contract/Exceptions/MessagesBase/`
