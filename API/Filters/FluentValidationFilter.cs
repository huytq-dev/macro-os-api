using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API;

public sealed class FluentValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
    {
        foreach (var arg in ctx.ActionArguments.Values.Where(v => v is not null))
        {
            var argType = arg!.GetType();
            var validatorType = typeof(IValidator<>).MakeGenericType(argType);
            var validator = ctx.HttpContext.RequestServices.GetService(validatorType) as dynamic;
            if (validator is null) continue;

            ValidationResult result = await validator.ValidateAsync((dynamic)arg);

            if (!result.IsValid)
            {
                var errors = result.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    );

                var response = ApiResponse<object>.Error(
                    message: "Validation failed",
                    statusCode: StatusCodes.Status400BadRequest,
                    errors: errors
                );

                ctx.Result = new BadRequestObjectResult(response);
                return;
            }
        }
        await next();
    }
}