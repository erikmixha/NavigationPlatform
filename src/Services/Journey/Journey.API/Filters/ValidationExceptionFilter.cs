using FluentValidation;
using Shared.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Journey.API.Filters;

public sealed class ValidationExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not ValidationException validationException)
        {
            return;
        }

        var errors = validationException.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        var problemDetails = ProblemDetailsResponse.CreateValidation(
            errors,
            context.HttpContext.TraceIdentifier,
            context.HttpContext.Request.Path);

        context.Result = new BadRequestObjectResult(problemDetails);
        context.ExceptionHandled = true;
    }
}

