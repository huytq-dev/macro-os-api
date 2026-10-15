# CLAUDE.md

## Commands
```bash
dotnet build
dotnet run --project src/API
dotnet ef migrations add <Name> -p src/Infrastructure -s src/API
dotnet ef database update -p src/Infrastructure -s src/API
```
Swagger: `https://localhost:7248/swagger` (Development). Cấu hình + secret: `src/API/appsettings.json` (không dùng `.env`).

## Architecture (.NET 10, mirror HRM-Solution)
`src/API` → `src/Application` → `src/Domain` · `src/Infrastructure` implement interface của Application · `src/Contract` dùng chung.

| Project | Chứa |
|---------|------|
| API | `Base/BaseApiController`, `Controllers/{Module}/`, `DependencyInjection/`, `Middlewares/GlobalExceptionHandler` |
| Application | `Features/{Module}/{UseCase}/` (Command/Query/Handler/Validator, DTO + MappingConfig cùng folder), `Interfaces/{Repositories,Services,External}/`, `Behaviors/` |
| Domain | `Abstractions/` (`EntityBase<T>`, `AuditableEntityBase<T>`, `Interface/{IEntity,IAuditable,ISoftDeletable}`), `Entities/{Module}/`, `Enums/{Module}/` |
| Infrastructure | `Persistence/{ApplicationDbContext, Configurations/{Base,Module}, Interceptors, Seed}`, `Implementations/{Repositories,Services}/`, `ExternalServices/{Cache,Email,Hashing,Storage,BackgroundJob}/`, `DependencyInjections/` |
| Contract | `ApiResponse<T>`, `Responses/Query/{QueryInfo,QueryResult,OrderType}`, `Exceptions/`, `Settings/`, `Models/`, `RegisterServiceAttribute` (không tham chiếu Domain) |

**Flat namespace** — namespace = tên project (`namespace Domain;`), không sub-namespace. Folder chỉ để gom theo module.

## Patterns

**DI** — `[RegisterService(typeof(IService))]`; scan tự động. `IGenericRepository<>` và `IUnitOfWork` đăng ký sẵn.

**Repository** — `IUnitOfWork.Repository<T>()` → `IGenericRepository<T>`:
- Đọc: `FindAsync(pred, ct)` / `FindAsync(pred, ct, x => x.Nav)` (no-tracking), `GetAllAsync`, `GetPagedAsync(QueryInfo)`.
- Sửa: `FindTrackedAsync(pred, ct)` rồi đổi field, `SaveChangesAsync` — không gọi `Update()`.
- Query phức tạp: `Query(tracking: true).Include(...).FirstOrDefaultAsync(...)` (EF cho phép trong Application, như HRM).

**Audit** — `AuditSaveChangesInterceptor` tự gán `CreatedAt/ModifiedAt`, `CreatedBy/UpdatedBy` (`IAuditable`), soft delete (`ISoftDeletable`). Không gán tay. `TimeProvider` thay `DateTime.UtcNow` trong code mới.

**Controller** — kế thừa `BaseApiController` (đã `[ApiController][Authorize]`, có `Sender`); `[AllowAnonymous]` cho endpoint public. UserId luôn từ JWT: `User.GetUserId()`, không nhận từ body. Request DTO cần enum Domain đặt trong `Application/Features/...`, còn lại ở `Contract/Models/`.

**Response** — `ApiResponse<T>.Ok / OkMessage / Fail`. Exception: throw `AppException` con (`NotFound/BadRequest/Conflict/Unauthorized/Forbidden`), `GlobalExceptionHandler` convert.

**Config** — typed options trong `Contract/Settings/`, bind bằng `services.Configure<T>(section)`, inject `IOptions<T>`. `JwtOptions.EnsureValid()` chặn khởi động nếu thiếu key.

**Mapster** — mapping config `IRegister` đặt cạnh feature (`Features/Users/UserMappingConfig.cs`); inject `IMapper`, không `.Adapt<T>()` inline.

**Background jobs** — `IBackgroundJobService.EnqueueEmailAsync(EmailContent)` (Quartz).

## C# Style
- Primary constructor cho class có DI; `sealed` cho handler/service; `sealed record` cho Command/Query/Response.
- Messages không hardcode: `ValidationMessages` (validator), `ExceptionMessages` (throw), `BusinessMessages` (success). Ở `Contract/Exceptions/Messages.cs`.

## Thêm 1 feature CRUD (checklist)
```
[ ] Domain/Entities/{Module}/{Entity}.cs  (+ Enums/{Module}/)
[ ] Infrastructure/Persistence/Configurations/{Module}/{Entity}Configuration.cs
[ ] Migration
[ ] Application/Features/{Module}/{UseCase}/  Command|Query + Handler + Validator (+ Dto, MappingConfig ở Features/{Module}/)
[ ] API/Controllers/{Module}/{Entity}Controller.cs : BaseApiController
```
