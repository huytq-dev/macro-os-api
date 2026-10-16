# CLAUDE.md

## Commands
```bash
dotnet build
dotnet run --project src/API
dotnet ef migrations add <Name> -p src/Infrastructure -s src/API
dotnet ef database update -p src/Infrastructure -s src/API
```
Swagger: `https://localhost:7000/swagger` (Development). Cấu hình chung: `src/API/appsettings.json` (không secret, không dùng `.env`). **Secret** (DB, JWT, SMTP, Cloudinary, Upstash) ở `src/API/appsettings.Local.json` — file này gitignore; copy từ `appsettings.Local.example.json` rồi điền. Thiếu secret → BE không khởi động (`JwtOptions.EnsureValid`, connection string rỗng).

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

**Controller** — kế thừa `BaseApiController` (đã `[ApiController][Authorize]`, có `Sender`); `[AllowAnonymous]` cho endpoint public. Bind thẳng Command/Query từ body, đặt tên tham số `cmd`/`query`; action là expression body một dòng `=> Ok(ApiResponse<T>.Ok(await Sender.Send(...)))`, chữ ký dài thì xuống dòng thụt 8 spaces; command không trả dữ liệu dùng `ApiResponse<Unit>`; mỗi controller có `/// <summary>` cấp class và mỗi action có `/// <summary>` tiếng Việt; nhóm action bằng `// --- Tên nhóm ---`. **Không** lấy UserId trong controller — handler inject `IUserContext.UserId`. Response model (`*Response`, `*Model`) ở `Contract/Models/<Domain>/`.

**Response** — `ApiResponse<T>.Ok / Fail` (message tuỳ biến truyền qua tham số `message` của `Ok`). Exception: throw `AppException` con (`NotFound/BadRequest/Conflict/Unauthorized/Forbidden`), `GlobalExceptionHandler` convert.

**Token xác thực/đặt lại mật khẩu** — lưu qua `IRedisCacheService`. Có `Upstash:BaseUrl` + `Token` thì dùng Redis Upstash; để trống thì dùng `MemoryCacheService` (bộ nhớ tiến trình, mất khi app khởi động lại, chỉ hợp 1 instance). Email xác thực chứa link `{Frontend:BaseUrl}/confirm-email?token=...` (`appsettings.json` = địa chỉ deploy `https://macro-os-five.vercel.app`, `appsettings.Development.json` ghi đè về `http://localhost:3000`; `AllowedOrigins` đặt theo cùng quy tắc) (24 giờ, dùng một lần); `confirm-email` và `resend-verification` là endpoint công khai vì đăng nhập bị chặn tới khi xác thực.

**Config** — typed options trong `Contract/Settings/`, bind bằng `services.Configure<T>(section)`, inject `IOptions<T>`. `JwtOptions.EnsureValid()` chặn khởi động nếu thiếu key.

**Mapster** — mapping config `IRegister` đặt cạnh feature (`Features/Users/UserMappingConfig.cs`); dùng `entity.Adapt<T>()` / `query.ProjectToType<T>()` trực tiếp, **không** inject `IMapper`.

**Quy tắc đầy đủ** — `BE/.rules` (bản HRM-Solution, đã chỉnh .NET 10). Đọc trước khi viết feature mới.

**Log** — Serilog (`API/DependencyInjection/Extensions/SerilogExtensions.cs`, như HRM): Console qua `Serilog.Sinks.Async` + sink Seq (`Seq:Url`, mặc định `http://localhost:1412`; Seq không chạy thì bỏ qua, không ảnh hưởng app). Mức log và override ở mục `Serilog` trong `appsettings.json`. Inject `ILogger<T>` như thường; không `Console.WriteLine`. Request chậm: `PerformancePipelineBehavior` (`Application:PerformanceThresholdMs`); command chứa mật khẩu/token đánh dấu `ISensitiveRequest` để không bao giờ ghi nội dung vào log.

**Background jobs** — `IBackgroundJobService.EnqueueEmailAsync(EmailContent)` (Quartz).

## C# Style
- Primary constructor cho class có DI; `sealed` cho handler/service; `sealed record` cho Command/Query/Response.
- Messages không hardcode: `ValidationMessages` (validator), `ExceptionMessages` (throw), `BusinessMessages` (success, dùng ở handler khi cần). Ở `Contract/Exceptions/Messages.cs`.

## Thêm 1 feature CRUD (checklist)
```
[ ] Domain/Entities/{Module}/{Entity}.cs  (+ Enums/{Module}/)
[ ] Infrastructure/Persistence/Configurations/{Module}/{Entity}Configuration.cs
[ ] Migration
[ ] Application/Features/{Module}/{UseCase}/  Command|Query + Handler + Validator (+ Dto, MappingConfig ở Features/{Module}/)
[ ] API/Controllers/{Module}/{Entity}Controller.cs : BaseApiController
```
