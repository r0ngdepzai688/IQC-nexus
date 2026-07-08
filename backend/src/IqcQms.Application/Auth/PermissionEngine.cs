using System.Collections.Generic;
using IqcQms.Domain.Entities.Auth;

namespace IqcQms.Application.Auth
{
    public interface IPermissionEngine
    {
        bool HasAccessToProject(User user, string partId);
        bool HasAccessToScope(User user, string partId, string scopeId);
        bool CanApproveTask(User user, string taskId);
    }

    public class PermissionEngine : IPermissionEngine
    {
        public bool HasAccessToProject(User user, string partId)
        {
            if (user.SystemRole == "System Admin") return true;
            if (user.Position == "IQC Group Leader" && user.Organization == "IQC Group") return true;
            if (user.Position == "Part Leader") return user.Part == partId;
            if (user.Position == "Cell Leader") return user.Part == partId;
            // Staff can access own projects
            return false;
        }

        public bool HasAccessToScope(User user, string partId, string scopeId)
        {
            if (user.SystemRole == "System Admin") return true;
            if (user.Position == "IQC Group Leader") return true;
            if (user.Position == "Part Leader") return user.Part == partId;
            if (user.Position == "Cell Leader") return user.Part == partId && user.Scope == scopeId;
            // Staff only assigned tasks
            return false;
        }

        public bool CanApproveTask(User user, string taskId)
        {
            if (user.SystemRole == "System Admin") return true;
            return user.Position == "IQC Group Leader" || user.Position == "Part Leader" || user.Position == "Cell Leader";
        }
    }
}
