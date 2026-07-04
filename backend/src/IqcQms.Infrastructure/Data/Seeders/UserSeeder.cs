using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ExcelDataReader;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IqcQms.Domain.Entities.Auth;
using IqcQms.Infrastructure.Data;

namespace IqcQms.Infrastructure.Data.Seeders
{
    public static class UserSeeder
    {
        public static async Task SyncUsersFromExcelAsync(AppDbContext context, string excelFilePath, ILogger logger)
        {
            if (!File.Exists(excelFilePath))
            {
                logger.LogWarning($"User Database file not found at {excelFilePath}. Skipping user sync.");
                return;
            }

            try
            {
                // Ensure ExcelDataReader is configured for .NET Core
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

                using var stream = File.Open(excelFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = ExcelReaderFactory.CreateReader(stream);
                
                var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = false // We will use indices directly
                    }
                });

                if (result.Tables.Count == 0)
                {
                    logger.LogWarning("No data tables found in Excel file.");
                    return;
                }

                var dataTable = result.Tables[0];
                int addedCount = 0;
                int updatedCount = 0;

                var existingUsers = await context.Users.ToDictionaryAsync(u => u.Username);
                var defaultRole = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "User");
                int? defaultRoleId = defaultRole?.Id;

                // Track processed users to avoid duplicates from the Excel file itself
                var processedUsernames = new HashSet<string>();

                // Skip header row
                for (int i = 1; i < dataTable.Rows.Count; i++)
                {
                    var row = dataTable.Rows[i];
                    var maNhanVien = row[1]?.ToString()?.Trim(); // Column 1: Mã nhân viên
                    if (string.IsNullOrEmpty(maNhanVien) || maNhanVien == "Mã nhân viên") continue;

                    // Prevent processing the same employee ID multiple times
                    if (processedUsernames.Contains(maNhanVien)) continue;
                    processedUsernames.Add(maNhanVien);

                    var ten = row[2]?.ToString()?.Trim() ?? string.Empty; // Column 2: Tên
                    var boPhan = row[3]?.ToString()?.Trim() ?? string.Empty; // Column 3: Bộ phận
                    var toChuc = row[4]?.ToString()?.Trim() ?? string.Empty; // Column 4: Tổ chức
                    var clName = row[5]?.ToString()?.Trim() ?? string.Empty; // Column 5: CL Name
                    var knoxId = row[6]?.ToString()?.Trim() ?? string.Empty; // Column 6: Knox ID
                    var position = row[7]?.ToString()?.Trim() ?? string.Empty; // Column 7: Chức vụ
                    var scope = row[8]?.ToString()?.Trim() ?? string.Empty; // Column 8: Scope

                    // Remove trailing semicolons in Knox ID if present
                    if (knoxId.EndsWith(";"))
                    {
                        knoxId = knoxId.TrimEnd(';');
                    }

                    var systemRole = maNhanVien == "10545998" ? "Administrator" : "User";
                    var dashboardProfile = "Auto";

                    if (existingUsers.TryGetValue(maNhanVien, out var user))
                    {
                        // Update existing user
                        bool isUpdated = false;
                        if (user.FullName != ten) { user.FullName = ten; isUpdated = true; }
                        if (user.Department != boPhan) { user.Department = boPhan; isUpdated = true; }
                        if (user.Organization != toChuc) { user.Organization = toChuc; isUpdated = true; }
                        if (user.ClName != clName) { user.ClName = clName; isUpdated = true; }
                        if (user.KnoxId != knoxId) { user.KnoxId = knoxId; isUpdated = true; }
                        if (user.Position != position) { user.Position = position; isUpdated = true; }
                        if (user.Scope != scope) { user.Scope = scope; isUpdated = true; }
                        if (user.SystemRole != systemRole) { user.SystemRole = systemRole; isUpdated = true; }
                        if (user.DashboardProfile != dashboardProfile) { user.DashboardProfile = dashboardProfile; isUpdated = true; }
                        
                        if (isUpdated) updatedCount++;
                    }
                    else
                    {
                        // Add new user
                        var newUser = new User
                        {
                            Username = maNhanVien,
                            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Welcome@123"), // Default password
                            FullName = ten,
                            Department = boPhan,
                            Organization = toChuc,
                            ClName = clName,
                            KnoxId = knoxId,
                            Position = position,
                            Scope = scope,
                            SystemRole = systemRole,
                            DashboardProfile = dashboardProfile,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow,
                            RoleId = defaultRoleId
                        };
                        context.Users.Add(newUser);
                        addedCount++;
                    }
                }

                await context.SaveChangesAsync();
                logger.LogInformation($"User sync completed successfully. Added: {addedCount}, Updated: {updatedCount}.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while syncing users from Excel.");
            }
        }
    }
}
