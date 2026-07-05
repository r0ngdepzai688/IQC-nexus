using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using IqcQms.Infrastructure.Data;
using IqcQms.Domain.Entities.Auth;

namespace IqcQms.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Username and password are required." });
            }

            // Find user by Username (Mã nhân viên) or KnoxId
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username || u.KnoxId == request.Username);

            if (user == null || !user.IsActive)
            {
                return Unauthorized(new { message = "Tài khoản không tồn tại hoặc đã bị khóa." });
            }

            // Verify password using BCrypt
            bool isPasswordValid = false;
            try
            {
                isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            }
            catch
            {
                // In case of invalid hash format or missing hash
                return Unauthorized(new { message = "Lỗi xác thực mật khẩu. Vui lòng liên hệ Admin." });
            }

            if (!isPasswordValid)
            {
                return Unauthorized(new { message = "Mật khẩu không chính xác." });
            }

            // Generate JWT Token
            var token = GenerateJwtToken(user);

            return Ok(new LoginResponse
            {
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    FullName = user.FullName,
                    Department = user.Department,
                    KnoxId = user.KnoxId
                }
            });
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["Secret"] ?? string.Empty;
            
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim("FullName", user.FullName ?? ""),
                new Claim("Department", user.Department ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings["ExpiryMinutes"] ?? "60")),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || 
                string.IsNullOrWhiteSpace(request.OldPassword) || 
                string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest(new { message = "Thiếu thông tin bắt buộc." });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null || !user.IsActive)
            {
                return NotFound(new { message = "Không tìm thấy người dùng." });
            }

            bool isOldPasswordValid = false;
            try
            {
                isOldPasswordValid = BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash);
            }
            catch
            {
                return BadRequest(new { message = "Lỗi xác thực mật khẩu cũ." });
            }

            if (!isOldPasswordValid)
            {
                return BadRequest(new { message = "Mật khẩu cũ không chính xác." });
            }

            // Hash new password and save
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedDate = DateTime.UtcNow;
            
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đổi mật khẩu thành công." });
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = new UserDto();
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string KnoxId { get; set; } = string.Empty;
    }

    public class ChangePasswordRequest
    {
        public string Username { get; set; } = string.Empty;
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
