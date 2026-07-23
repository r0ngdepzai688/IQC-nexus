using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
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

        if (ParsePermissions(user.Role).Contains(requirement.Permission))
            authorizationContext.Succeed(requirement);
    }

    private static HashSet<string> ParsePermissions(Role? role)
    {
        if (string.IsNullOrWhiteSpace(role?.Permissions))
            return [];

        try
        {
            var values = JsonSerializer.Deserialize<string[]>(role.Permissions);
            if (values is not null)
                return values.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            // Backward-compatible support for existing comma/semicolon-delimited role data.
        }

        return role.Permissions
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
