using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using ScholarshipRequest.Api.Data.Identity;
using ScholarshipRequest.Api.Security;
using ScholarshipRequest.Shared.V1.Authentication;

namespace ScholarshipRequest.Api.Features.StaffAuthentication;

public static class StaffAuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapStaffAuthenticationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/auth")
            .WithTags("Staff authentication");

        group.MapGet("/antiforgery-token", GetAntiforgeryToken)
            .AllowAnonymous()
            .WithName("GetStaffAntiforgeryToken");

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .AddEndpointFilter<AntiforgeryValidationFilter>()
            .WithName("StaffLogin");

        group.MapGet("/session", GetSessionAsync)
            .AllowAnonymous()
            .WithName("GetStaffSession");

        group.MapPost("/logout", LogoutAsync)
            .RequireAuthorization()
            .AddEndpointFilter<AntiforgeryValidationFilter>()
            .WithName("StaffLogout");

        return endpoints;
    }

    private static IResult GetAntiforgeryToken(
        HttpContext httpContext,
        IAntiforgery antiforgery)
    {
        DisableCaching(httpContext.Response);
        var tokens = antiforgery.GetAndStoreTokens(httpContext);

        return TypedResults.Ok(new AntiforgeryTokenResponse(
            tokens.RequestToken ?? throw new InvalidOperationException(
                "The antiforgery service did not issue a request token."),
            AuthenticationConstants.AntiforgeryHeaderName));
    }

    private static async Task<IResult> LoginAsync(
        StaffLoginRequest request,
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        DisableCaching(httpContext.Response);
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(
                errors,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "VALIDATION_FAILED"
                });
        }

        var user = await userManager.FindByNameAsync(request.UserName.Trim());
        if (user is null || !user.IsActive)
        {
            return InvalidCredentials();
        }

        var passwordResult = await signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);
        if (!passwordResult.Succeeded ||
            !await userManager.IsInRoleAsync(user, AuthenticationConstants.StaffRole))
        {
            return InvalidCredentials();
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        return TypedResults.Ok(await CreateSessionAsync(userManager, user));
    }

    private static async Task<IResult> GetSessionAsync(
        HttpContext httpContext,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        DisableCaching(httpContext.Response);
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return TypedResults.Ok(StaffSessionResponse.Anonymous);
        }

        var user = await userManager.GetUserAsync(httpContext.User);
        if (user is null ||
            !user.IsActive ||
            !await userManager.IsInRoleAsync(user, AuthenticationConstants.StaffRole))
        {
            await signInManager.SignOutAsync();
            return TypedResults.Ok(StaffSessionResponse.Anonymous);
        }

        return TypedResults.Ok(await CreateSessionAsync(userManager, user));
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext httpContext,
        SignInManager<ApplicationUser> signInManager)
    {
        DisableCaching(httpContext.Response);
        await signInManager.SignOutAsync();
        return TypedResults.NoContent();
    }

    internal static async Task<StaffSessionResponse> CreateSessionAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new StaffSessionResponse(
            true,
            user.Id,
            user.UserName,
            user.FullName,
            roles.Order(StringComparer.Ordinal).ToArray());
    }

    private static Dictionary<string, string[]> Validate(StaffLoginRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            errors[nameof(request.UserName)] = ["กรุณาระบุชื่อผู้ใช้"];
        }
        else if (request.UserName.Length > 100)
        {
            errors[nameof(request.UserName)] = ["ชื่อผู้ใช้ต้องไม่เกิน 100 ตัวอักษร"];
        }

        if (string.IsNullOrEmpty(request.Password))
        {
            errors[nameof(request.Password)] = ["กรุณาระบุรหัสผ่าน"];
        }
        else if (request.Password.Length > 200)
        {
            errors[nameof(request.Password)] = ["รหัสผ่านต้องไม่เกิน 200 ตัวอักษร"];
        }

        return errors;
    }

    private static IResult InvalidCredentials() =>
        Results.Problem(
            title: "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง",
            statusCode: StatusCodes.Status401Unauthorized,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = "AUTH_INVALID_CREDENTIALS"
            });

    private static void DisableCaching(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store";
        response.Headers.Pragma = "no-cache";
    }
}
