using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;

namespace PMT.Api.Policies;

public sealed class CanModifyHandler : AuthorizationHandler<CanModifyRequirement, int> {
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CanModifyRequirement requirement, int targetId) {
        if (context.User.IsInRole("Admin") || context.User.IsInRole("Management")) {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (!int.TryParse(context.User.FindFirstValue(JwtRegisteredClaimNames.Sub), out int myId))
            return Task.CompletedTask;  // Failed to convert to int

        if (myId == targetId) {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
