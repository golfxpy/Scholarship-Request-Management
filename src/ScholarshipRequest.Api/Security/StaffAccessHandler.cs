using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using ScholarshipRequest.Api.Data.Identity;
using ScholarshipRequest.Shared.V1.Authentication;

namespace ScholarshipRequest.Api.Security;

public sealed class StaffAccessHandler(UserManager<ApplicationUser> userManager)
    : AuthorizationHandler<StaffAccessRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        StaffAccessRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var user = await userManager.GetUserAsync(context.User);
        if (user is not null &&
            user.IsActive &&
            await userManager.IsInRoleAsync(user, AuthenticationConstants.StaffRole))
        {
            context.Succeed(requirement);
        }
    }
}
