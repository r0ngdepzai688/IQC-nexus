namespace IqcQms.Application.Auth;

public static class PlatformPermissions
{
    public const string DashboardView = "dashboard.view";
    public const string ImportView = "import.view";
    public const string ImportCreate = "import.create";
    public const string ImportReview = "import.review";
    public const string ImportCommit = "import.commit";
    public const string ImportAdmin = "import.admin";
    public const string DownloadView = "download.view";
    public const string DownloadManage = "download.manage";
    public const string UserManage = "user.manage";
    public const string RoleManage = "role.manage";
    public const string AuditView = "audit.view";

    public static readonly IReadOnlyList<string> All =
    [
        DashboardView, ImportView, ImportCreate, ImportReview, ImportCommit,
        ImportAdmin, DownloadView, DownloadManage, UserManage, RoleManage, AuditView
    ];
}
