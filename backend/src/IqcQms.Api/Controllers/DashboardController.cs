using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IqcQms.Infrastructure.Data;
using System.Threading.Tasks;

namespace IqcQms.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var totalParts = await _context.Parts.CountAsync();
            var activeStandards = await _context.InspectionStandards.CountAsync(s => s.IsActive);
            var totalEmployees = await _context.Employees.CountAsync();
            var totalEquipments = await _context.Equipments.CountAsync();

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
