var builder = WebApplication.CreateBuilder(args);

builder.Host.AddSerilogLogging();

builder.Services
    .AddInfrastructureServices(builder.Configuration)
    .AddApplicationServices(builder.Configuration)
    .AddApiServices(builder.Configuration);

var app = builder.Build();

// Swagger mở ở mọi môi trường (kể cả production) theo yêu cầu: /swagger
app.UseSwagger();
app.UseSwaggerUI();

await app.ApplyMigrationsAsync();

app.UseStartupBanner();
app.UseApiMiddlewares();
app.MapControllers();
app.MapApiHealthChecks();

app.Run();
