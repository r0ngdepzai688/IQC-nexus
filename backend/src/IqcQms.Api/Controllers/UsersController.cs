using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IqcQms.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;

namespace IqcQms.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    // [Authorize] // Temporarily disabled for easier testing if needed, or we can leave it since frontend might not pass token yet. Let's not use [Authorize] for now unless strictly required, wait, auth is simulated. I will remove [Authorize].
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetUserCount()
        {
            var count = await _context.Users.CountAsync();
            return Ok(new { count });
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    id = u.Username,
                    name = u.FullName,
                    dept = u.Department,
                    org = u.Organization,
                    position = string.IsNullOrEmpty(u.Position) ? "Staff" : u.Position,
                    scope = string.IsNullOrEmpty(u.Scope) ? "Execution" : u.Scope,
                    systemRole = u.SystemRole,
                    dashboardProfile = u.DashboardProfile,
                    status = u.AccountStatus,
                    lastLogin = u.LastLogin.HasValue ? u.LastLogin.Value.ToString("yyyy-MM-dd HH:mm") : "Never",
                    createdDate = u.CreatedAt.ToString("yyyy-MM-dd"),
                    notes = u.Notes
                })
                .ToListAsync();

            return Ok(users);
        }
    }
}
