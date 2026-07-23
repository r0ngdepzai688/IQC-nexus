using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IqcQms.Infrastructure.Data;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using IqcQms.Application.Auth;

namespace IqcQms.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = PlatformPermissions.DashboardView)]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardStats(CancellationToken cancellationToken)
        {
            var totalParts = await _context.Parts.CountAsync(cancellationToken);
            var activeStandards = await _context.InspectionStandards.CountAsync(s => s.IsActive, cancellationToken);
            var totalEmployees = await _context.Employees.CountAsync(cancellationToken);
            var totalEquipments = await _context.Equipments.CountAsync(cancellationToken);

            // Mocking trend data for now until we have actual inspection logs
            var passRate = 98.2;
            var openNcrs = 24;

            return Ok(new
            {
                TotalParts = totalParts,
                ActiveStandards = activeStandards,
                TotalEmployees = totalEmployees,
                TotalEquipments = totalEquipments,
                PassRate = passRate,
                OpenNcrs = openNcrs,
                PendingApprovals = 7 // Mock value
            });
        }
    }
}
