using System.IdentityModel.Tokens.Jwt;
using IqcQms.Application.Auth;
using IqcQms.Domain.Entities.Auth;
using IqcQms.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace IqcQms.Api.Security;

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;

public sealed class PermissionAuthorizationHandler(AppDbContext context)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext authorizationContext,
        PermissionRequirement requirement)
    {
        var subject = authorizationContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? authorizationContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(subject, out var userId))
            return;

        var user = await context.Users.AsNoTracking().Include(value => value.Role)
            .SingleOrDefaultAsync(value => value.Id == userId);
        if (user is null || !user.IsActive ||
            !string.Equals(user.AccountStatus, "Active", StringComparison.OrdinalIgnoreCase))
            return;

        if (string.Equals(user.SystemRole, "Administrator", StringComparison.OrdinalIgnoreCase))
        {
            authorizationContext.Succeed(requirement);
            return;
        }

        if (RolePermissions.Parse(user.Role).Contains(requirement.Permission))
            authorizationContext.Succeed(requirement);
    }
}
