using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using IqcQms.Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IqcQms.Infrastructure.Data.Seeders
{
    public static partial class UserSeeder
    {
        private const string FixtureActor = "synthetic-fixture";
        private static readonly HashSet<string> AccountStatuses = new(StringComparer.Ordinal)
        {
            "Active", "Inactive", "Pending", "Locked"
        };

        public static async Task SyncUsersFromSyntheticFixtureAsync(
            AppDbContext context,
            string fixturePath,
            string? seedPassword,
            ILogger logger)
        {
            var records = await LoadAndValidateFixtureAsync(fixturePath);
            await SyncValidatedUsersAsync(context, records, seedPassword, logger);
        }

        public static async Task ValidateMigrateAndSyncAsync(
            AppDbContext context,
            string fixturePath,
            string? seedPassword,
            ILogger logger,
            Func<Task> migrateAsync)
        {
            var records = await LoadAndValidateFixtureAsync(fixturePath);
            await migrateAsync();
            await SyncValidatedUsersAsync(context, records, seedPassword, logger);
        }

        public static async Task SyncValidatedUsersAsync(
            AppDbContext context,
            IReadOnlyList<SyntheticPersonnelRecord> records,
            string? seedPassword,
            ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(seedPassword))
            {
                logger.LogWarning(
                    "Validated {Count} synthetic personnel records, but user seeding is disabled because no external seed credential was provided.",
                    records.Count);
                return;
            }

            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                var existingUsers = (await context.Users.ToListAsync())
                    .ToDictionary(user => user.Username, StringComparer.Ordinal);
                var defaultRoleId = await context.Roles
                    .Where(role => role.RoleName == "User")
                    .Select(role => (int?)role.Id)
                    .FirstOrDefaultAsync();
                var now = DateTime.UtcNow;
                var addedCount = 0;
                var updatedCount = 0;

                foreach (var record in records)
                {
                    if (existingUsers.TryGetValue(record.EmployeeId, out var user))
                    {
                        ApplyRecord(user, record);
                        user.UpdatedBy = FixtureActor;
                        user.UpdatedDate = now;
                        updatedCount++;
                        continue;
                    }

                    user = new User
                    {
                        Username = record.EmployeeId,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(seedPassword),
                        CreatedAt = now,
                        CreatedBy = FixtureActor,
                        RoleId = defaultRoleId
                    };
                    ApplyRecord(user, record);
                    context.Users.Add(user);
                    existingUsers.Add(record.EmployeeId, user);
                    addedCount++;
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();
                logger.LogInformation(
                    "Synthetic user sync completed atomically. Added: {AddedCount}, Updated: {UpdatedCount}.",
                    addedCount,
                    updatedCount);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public static async Task<IReadOnlyList<SyntheticPersonnelRecord>> LoadAndValidateFixtureAsync(
            string fixturePath)
        {
            if (!File.Exists(fixturePath))
            {
                throw new FileNotFoundException("Synthetic personnel fixture was not found.", fixturePath);
            }

            await using var stream = File.OpenRead(fixturePath);
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = false,
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
            };
            var records = await JsonSerializer.DeserializeAsync<List<SyntheticPersonnelRecord>>(stream, options)
                ?? throw new InvalidDataException("Synthetic personnel fixture must be a JSON array.");

            ValidateRecords(records);
            return records;
        }

        private static void ValidateRecords(IReadOnlyList<SyntheticPersonnelRecord> records)
        {
            if (records.Count == 0)
            {
                throw new InvalidDataException("Synthetic personnel fixture must not be empty.");
            }

            var employeeIds = new HashSet<string>(StringComparer.Ordinal);
            var knoxIds = new HashSet<string>(StringComparer.Ordinal);

            for (var index = 0; index < records.Count; index++)
            {
                var record = records[index];
                RequireString(record.EmployeeId, nameof(record.EmployeeId), index);
                RequireString(record.FullName, nameof(record.FullName), index);
                RequireString(record.Department, nameof(record.Department), index);
                RequireString(record.Organization, nameof(record.Organization), index);
                RequireString(record.ClName, nameof(record.ClName), index);
                RequireString(record.Position, nameof(record.Position), index);
                RequireString(record.Scope, nameof(record.Scope), index);
                RequireString(record.SystemRole, nameof(record.SystemRole), index);
                RequireString(record.DashboardProfile, nameof(record.DashboardProfile), index);
                RequireString(record.AccountStatus, nameof(record.AccountStatus), index);
                ValidateOptional(record.PreferredName, nameof(record.PreferredName), index);
                ValidateOptional(record.KnoxId, nameof(record.KnoxId), index);
                ValidateOptional(record.Email, nameof(record.Email), index);
                ValidateOptional(record.RoleProfile, nameof(record.RoleProfile), index);
                ValidateOptional(record.Part, nameof(record.Part), index);
                ValidateOptional(record.Avatar, nameof(record.Avatar), index);
                ValidateOptional(record.Notes, nameof(record.Notes), index);

                if (!SyntheticIdRegex().IsMatch(record.EmployeeId))
                {
                    Invalid(index, nameof(record.EmployeeId), "must match ^SYN-[A-Z0-9-]+$");
                }
                if (!employeeIds.Add(record.EmployeeId))
                {
                    Invalid(index, nameof(record.EmployeeId), "must be unique using ordinal case-sensitive comparison");
                }
                if (record.KnoxId is not null && !knoxIds.Add(record.KnoxId))
                {
                    Invalid(index, nameof(record.KnoxId), "must be unique when present");
                }
                if (record.Email is not null && !SyntheticEmailRegex().IsMatch(record.Email))
                {
                    Invalid(index, nameof(record.Email), "must use the example.invalid domain");
                }
                if (record.SystemRole is not ("Administrator" or "User"))
                {
                    Invalid(index, nameof(record.SystemRole), "must be Administrator or User");
                }
                if (!AccountStatuses.Contains(record.AccountStatus))
                {
                    Invalid(index, nameof(record.AccountStatus), "has an unsupported value");
                }
                if (record.IsActive != (record.AccountStatus == "Active"))
                {
                    Invalid(index, nameof(record.IsActive), "must be true only when accountStatus is Active");
                }
                if (record.SystemRole != "User")
                {
                    Invalid(index, nameof(record.SystemRole), "canonical synthetic records must default to User");
                }
            }
        }

        private static void ApplyRecord(User user, SyntheticPersonnelRecord record)
        {
            user.FullName = record.FullName;
            user.Department = record.Department;
            user.Organization = record.Organization;
            user.ClName = record.ClName;
            user.KnoxId = record.KnoxId ?? string.Empty;
            user.Email = record.Email ?? string.Empty;
            user.Position = record.Position;
            user.Scope = record.Scope;
            user.SystemRole = record.SystemRole;
            user.DashboardProfile = record.DashboardProfile;
            user.AccountStatus = record.AccountStatus;
            user.IsActive = record.IsActive;
            user.RoleProfile = record.RoleProfile ?? string.Empty;
            user.Part = record.Part ?? string.Empty;
            user.Avatar = record.Avatar ?? string.Empty;
            user.Notes = record.Notes ?? string.Empty;
        }

        private static void RequireString(string value, string field, int index)
        {
            if (string.IsNullOrWhiteSpace(value) || value != value.Trim())
            {
                Invalid(index, field, "must be a non-empty trimmed string");
            }
            ValidateUnicode(value, field, index);
        }

        private static void ValidateOptional(string? value, string field, int index)
        {
            if (value is null)
            {
                return;
            }
            if (value.Length == 0 || value != value.Trim())
            {
                Invalid(index, field, "must be null or a non-empty trimmed string");
            }
            ValidateUnicode(value, field, index);
        }

        private static void ValidateUnicode(string value, string field, int index)
        {
            if (!value.IsNormalized(NormalizationForm.FormC) || value.Any(char.IsControl))
            {
                Invalid(index, field, "must be NFC Unicode without control characters");
            }
        }

        private static void Invalid(int index, string field, string message) =>
            throw new InvalidDataException($"Synthetic personnel record {index + 1}, field {field}: {message}.");

        [GeneratedRegex("^SYN-[A-Z0-9-]+$", RegexOptions.CultureInvariant)]
        private static partial Regex SyntheticIdRegex();

        [GeneratedRegex("^[^\\s@]+@example\\.invalid$", RegexOptions.CultureInvariant)]
        private static partial Regex SyntheticEmailRegex();
    }

    public sealed class SyntheticPersonnelRecord
    {
        public string EmployeeId { get; init; } = string.Empty;
        public string FullName { get; init; } = string.Empty;
        public string? PreferredName { get; init; }
        public string Department { get; init; } = string.Empty;
        public string Organization { get; init; } = string.Empty;
        public string ClName { get; init; } = string.Empty;
        public string? KnoxId { get; init; }
        public string? Email { get; init; }
        public string Position { get; init; } = string.Empty;
        public string Scope { get; init; } = string.Empty;
        public string SystemRole { get; init; } = string.Empty;
        public string DashboardProfile { get; init; } = string.Empty;
        public string AccountStatus { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public string? RoleProfile { get; init; }
        public string? Part { get; init; }
        public string? Avatar { get; init; }
        public string? Notes { get; init; }
    }
}
