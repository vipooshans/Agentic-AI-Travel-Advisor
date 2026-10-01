using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TravelAdvisor.Api.Infrastructure;

/// <summary>Runs the registered FluentValidation validator for each action argument before the action executes.</summary>
public sealed class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var errors = new Dictionary<string, List<string>>();
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null || argument.GetType().IsPrimitive || argument is string)
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                continue;

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
            foreach (var failure in result.Errors)
            {
                var key = JsonNamingPolicy.CamelCase.ConvertName(failure.PropertyName);
                if (!errors.TryGetValue(key, out var list))
                    errors[key] = list = [];
                list.Add(failure.ErrorMessage);
            }
        }

        if (errors.Count == 0)
        {
            await next();
            return;
        }

        context.Result = ValidationResponses.Create(context.HttpContext,
            errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
    }
}

public static class ValidationResponses
{
    public static IActionResult Create(HttpContext httpContext, IDictionary<string, string[]> errors)
    {
        var problem = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Instance = httpContext.Request.Path
        };
        var first = errors.Values.SelectMany(v => v).FirstOrDefault() ?? problem.Title;
        problem.Detail = first;
        problem.Extensions["message"] = first;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
    }

    public static IActionResult FromModelState(ActionContext context)
    {
        var errors = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0)
            .ToDictionary(
                e => string.IsNullOrEmpty(e.Key) ? "body" : JsonNamingPolicy.CamelCase.ConvertName(e.Key.TrimStart('$', '.')),
                e => e.Value!.Errors
                    .Select(err => e.Key.StartsWith('$') || string.IsNullOrWhiteSpace(err.ErrorMessage)
                        ? "The value is missing or has the wrong format."
                        : err.ErrorMessage)
                    .ToArray());
        return Create(context.HttpContext, errors);
    }
}
