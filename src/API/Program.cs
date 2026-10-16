var builder = WebApplication.CreateBuilder(args);

// Secret local (DB, JWT, SMTP, Cloudinary, Upstash): file này nằm trong .gitignore, ghi đè appsettings.json
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services
    .AddInfrastructureServices(builder.Configuration)
    .AddApplicationServices(builder.Configuration)
    .AddApiServices(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

await app.ApplyMigrationsAsync();

app.UseApiMiddlewares();
app.MapControllers();

app.Run();
