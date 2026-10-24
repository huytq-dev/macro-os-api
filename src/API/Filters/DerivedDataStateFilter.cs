using Microsoft.AspNetCore.Mvc.Filters;

namespace API;

/// <summary>
/// Gắn <see cref="DerivedDataStateHeader.Name"/> vào phản hồi của lệnh ghi thành công (xem <see cref="DerivedDataStateHeader.ShouldAttach"/>): đọc trạng thái dựng lại của user SAU khi handler xong
/// (dẫn xuất đã dựng đồng bộ = Ready, còn job nền = Updating, hết lượt thử = Failed). Không thể làm hỏng lệnh ghi đã thành công: lỗi đọc trạng thái chỉ bỏ header.
/// </summary>
public sealed class DerivedDataStateFilter(ISender sender, ILogger<DerivedDataStateFilter> logger) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // POST chỉ để tính (xem trước…): không có trạng thái dựng lại để báo, không tốn truy vấn
        if (context.ActionDescriptor.EndpointMetadata?.OfType<SkipDerivedDataStateAttribute>().Any() == true)
        {
            await next();
            return;
        }

        var executed = await next();
        if (executed.Exception is not null && !executed.ExceptionHandled) return;

        var http = context.HttpContext;
        var status = executed.Result switch
        {
            ObjectResult { StatusCode: { } code } => code,
            StatusCodeResult result => result.StatusCode,
            _ => StatusCodes.Status200OK,
        };
        if (!DerivedDataStateHeader.ShouldAttach(http.Request.Method, http.Request.Path.Value ?? string.Empty, status, http.User.Identity?.IsAuthenticated == true)) return;

        try
        {
            var state = await sender.Send(new GetDerivedDataStateQuery(), http.RequestAborted);   // một truy vấn (không đọc "lần xong cuối")
            http.Response.Headers[DerivedDataStateHeader.Name] = state;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Could not read derived data state for {Method} {Path}", http.Request.Method, http.Request.Path);
        }
    }
}
