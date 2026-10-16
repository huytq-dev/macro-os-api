namespace API;

public static class SerilogExtensions
{
    public static IHostBuilder AddSerilogLogging(this IHostBuilder host)
        => host.UseSerilog((ctx, cfg) => cfg
            .ReadFrom.Configuration(ctx.Configuration)
            // Console đồng bộ làm request chờ khi log nhiều; ghi qua hàng đợi nền.
            .WriteTo.Async(sink => sink.Console())
            .WriteTo.Seq(ctx.Configuration["Seq:Url"] ?? "http://localhost:1412"));
}
