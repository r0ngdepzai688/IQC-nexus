using System.Text.Json;
using IqcQms.Domain.Entities.Auth;

namespace IqcQms.Application.Auth;

public static class RolePermissions
{
    public static IReadOnlySet<string> Parse(Role? role)
    {
        if (string.IsNullOrWhiteSpace(role?.Permissions))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var values = JsonSerializer.Deserialize<string[]>(role.Permissions);
            if (values is not null)
                return values.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            // Existing deployments may still contain comma/semicolon-delimited values.
        }

        return role.Permissions
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}