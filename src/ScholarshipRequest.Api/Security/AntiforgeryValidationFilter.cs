using Microsoft.AspNetCore.Antiforgery;

namespace ScholarshipRequest.Api.Security;

public sealed class AntiforgeryValidationFilter(IAntiforgery antiforgery) : IEndpointFilter
{
    private static readonly HashSet<string> SafeMethods =
        new(StringComparer.OrdinalIgnoreCase)
        {
            HttpMethods.Get,
            HttpMethods.Head,
            HttpMethods.Options,
            HttpMethods.Trace
        };

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        if (SafeMethods.Contains(context.HttpContext.Request.Method))
        {
            return await next(context);
        }

        try
        {
            await antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            context.HttpContext.Response.Headers.Pragma = "no-cache";

            return Results.Problem(
                title: "ไม่สามารถยืนยันความปลอดภัยของคำขอได้",
                detail: "กรุณาโหลดหน้าใหม่แล้วลองอีกครั้ง",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "ANTIFORGERY_VALIDATION_FAILED"
                });
        }

        return await next(context);
    }
}
