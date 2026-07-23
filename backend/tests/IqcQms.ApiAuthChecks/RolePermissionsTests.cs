using IqcQms.Application.Auth;
using IqcQms.Domain.Entities.Auth;
using Xunit;
namespace IqcQms.ApiAuthChecks;
public sealed class RolePermissionsTests
{
    [Fact] public void MissingRoleHasNoImplicitPermissions() => Assert.Empty(RolePermissions.Parse(null));
    [Fact] public void JsonAndLegacyPermissionFormatsAreSupported()
    {
        Assert.Contains(PlatformPermissions.ImportView, RolePermissions.Parse(new Role { Permissions = "[\"import.view\"]" }));
        Assert.Contains(PlatformPermissions.AuditView, RolePermissions.Parse(new Role { Permissions = "audit.view;download.view" }));
    }
}