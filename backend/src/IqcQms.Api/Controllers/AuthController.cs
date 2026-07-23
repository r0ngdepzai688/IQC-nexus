using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IqcQms.Application.Auth;
using IqcQms.Domain.Entities.Auth;
using IqcQms.Domain.Entities.System;
using IqcQms.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace IqcQms.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public sealed class AuthController(AppDbContext context, IConfiguration configuration) : ControllerBase
{
    private const string InvalidCredentialsMessage = "Invalid username or password.";

    [AllowAnonymous]
    [EnableRateLimiting("login")]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { code = "AUTH_INPUT_INVALID", message = "Username and password are required." });

        var normalizedUsername = request.Username.Trim();
        var user = await context.Users.Include(value => value.Role).FirstOrDefaultAsync(
            value => value.Username == normalizedUsername || value.KnoxId == normalizedUsername,
            cancellationToken);

        var valid = false;
        if (user is not null && user.IsActive &&
            string.Equals(user.AccountStatus, "Active", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                valid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            }
            catch (Exception)
            {
                valid = false;
            }
        }

        if (!valid)
        {
            await WriteAuditAsync("LoginFailed", user, cancellationToken);
            return Unauthorized(new { code = "AUTH_CREDENTIALS_INVALID", message = InvalidCredentialsMessage });
        }

        user!.LastLogin = DateTime.UtcNow;
        context.AuditLogs.Add(CreateAudit("LoginSucceeded", user));
        await context.SaveChangesAsync(cancellationToken);
        return Ok(new LoginResponse { Token = GenerateJwtToken(user), User = UserDto.From(user) });
    }

    [HttpGet("me")]
    public async Task<IActionResult> CurrentUser(CancellationToken cancellationToken)
    {
        var user = await FindCurrentUserAsync(cancellationToken);
        return user is null
            ? Unauthorized(new { code = "AUTH_SESSION_INVALID", message = "Authentication session is no longer valid." })
            : Ok(UserDto.From(user));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var user = await FindCurrentUserAsync(cancellationToken);
        if (user is not null)
            await WriteAuditAsync("Logout", user, cancellationToken);

        // Bearer tokens are stateless. The client must discard the token; short expiry bounds reuse.
        return NoContent();
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OldPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { code = "AUTH_INPUT_INVALID", message = "Current and new passwords are required." });
        if (request.NewPassword.Length < 12)
            return BadRequest(new { code = "AUTH_PASSWORD_WEAK", message = "New password must be at least 12 characters." });

        var user = await FindCurrentUserAsync(cancellationToken);
        if (user is null)
            return Unauthorized(new { code = "AUTH_SESSION_INVALID", message = "Authentication session is no longer valid." });

        bool valid;
        try { valid = BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash); }
        catch (Exception) { valid = false; }
        if (!valid)
            return BadRequest(new { code = "AUTH_PASSWORD_CURRENT_INVALID", message = "Current password is incorrect." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedDate = DateTime.UtcNow;
        user.UpdatedBy = user.Username;
        context.AuditLogs.Add(CreateAudit("PasswordChanged", user));
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<User?> FindCurrentUserAsync(CancellationToken cancellationToken)
    {
        var subject = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(subject, out var id))
            return null;
        return await context.Users.Include(value => value.Role).SingleOrDefaultAsync(
            value => value.Id == id && value.IsActive && value.AccountStatus == "Active", cancellationToken);
    }

    private async Task WriteAuditAsync(string action, User? user, CancellationToken cancellationToken)
    {
        context.AuditLogs.Add(CreateAudit(action, user));
        await context.SaveChangesAsync(cancellationToken);
    }

    private AuditLog CreateAudit(string action, User? user) => new()
    {
        Timestamp = DateTime.UtcNow,
        AdminId = user?.Username ?? string.Empty,
        AdminName = user?.FullName ?? string.Empty,
        AffectedUserId = user?.Username ?? string.Empty,
        ActionType = action,
        IpAddress = ControllerContext.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
        OldValue = string.Empty,
        NewValue = string.Empty
    };

    private string GenerateJwtToken(User user)
    {
        var settings = configuration.GetSection("JwtSettings");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings["Secret"]!));
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.SystemRole),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        var token = new JwtSecurityToken(
            settings["Issuer"], settings["Audience"], claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(settings["ExpiryMinutes"] ?? "60")),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public sealed class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public sealed class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = new();
}

public sealed class UserDto
{
    public int Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string EmployeeId { get; init; } = string.Empty;
    public string Department { get; init; } = string.Empty;
    public string KnoxId { get; init; } = string.Empty;
    public string Position { get; init; } = string.Empty;
    public string Scope { get; init; } = string.Empty;
    public string SystemRole { get; init; } = "User";
    public string AccountStatus { get; init; } = "Active";
    public string Organization { get; init; } = string.Empty;
    public string Part { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string RoleProfile { get; init; } = string.Empty;
    public string Avatar { get; init; } = string.Empty;
    public IReadOnlyList<string> Roles { get; init; } = [];
    public IReadOnlyList<string> Permissions { get; init; } = [];

    public static UserDto From(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        FullName = user.FullName,
        EmployeeId = !string.IsNullOrWhiteSpace(user.Username) ? user.Username : user.KnoxId,
        Department = user.Department,
        KnoxId = user.KnoxId,
        Position = user.Position,
        Scope = user.Scope,
        SystemRole = user.SystemRole,
        AccountStatus = user.AccountStatus,
        Organization = user.Organization,
        Part = user.Part,
        Email = user.Email,
        RoleProfile = user.RoleProfile,
        Avatar = user.Avatar,
        Roles = string.IsNullOrWhiteSpace(user.Role?.RoleName) ? [user.SystemRole] : [user.Role.RoleName],
        Permissions = RolePermissions.Parse(user.Role).Order(StringComparer.OrdinalIgnoreCase).ToArray()
    };
}

public sealed class ChangePasswordRequest
{
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
