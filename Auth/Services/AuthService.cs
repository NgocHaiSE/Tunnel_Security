using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using TunnelSecurity.Auth.DTOs;
using TunnelSecurity.Data.Auth;
using TunnelSecurity.Data.Auth.Models;

namespace TunnelSecurity.Auth.Services
{
    public class AuthService : IAuthService
    {
        private readonly AuthDbContext _db;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly JwtSettings _jwt;

        public AuthService(
            AuthDbContext db,
            IPasswordHasher<User> passwordHasher,
            IOptions<JwtSettings> jwtOptions)
        {
            _db = db;
            _passwordHasher = passwordHasher;
            _jwt = jwtOptions.Value;
        }

        public async Task RegisterAsync(RegisterRequest request, CancellationToken ct = default)
        {
            if (await _db.Users.AnyAsync(u => u.Username == request.Username || u.Email == request.Email, ct))
                throw new InvalidOperationException("Username or email already exists.");

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                EmailVerified = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
        {
            var user = await _db.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == request.UsernameOrEmail || u.Email == request.UsernameOrEmail, ct);

            if (user == null)
                throw new UnauthorizedAccessException("Invalid credentials.");

            var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash ?? string.Empty, request.Password);
            if (verify == PasswordVerificationResult.Failed)
                throw new UnauthorizedAccessException("Invalid credentials.");

            return await CreateTokensForUserAsync(user, ct);
        }

        public async Task<LoginResponse> RefreshAsync(string refreshToken, CancellationToken ct = default)
        {
            var tokenHash = HashToken(refreshToken);
            var existing = await _db.RefreshTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

            if (existing == null || existing.Revoked || existing.ExpiresAt <= DateTimeOffset.UtcNow)
                throw new UnauthorizedAccessException("Invalid refresh token.");

            // rotate refresh token
            existing.Revoked = true;
            var newRefreshValue = GenerateSecureToken();
            var newRefresh = new RefreshToken
            {
                Id = Guid.NewGuid(), // ensure client-side id so we can reference it immediately
                UserId = existing.UserId,
                TokenHash = HashToken(newRefreshValue),
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwt.RefreshTokenDays),
                Revoked = false
            };

            _db.RefreshTokens.Add(newRefresh);
            existing.ReplacedByToken = newRefresh.Id;
            await _db.SaveChangesAsync(ct);

            var response = await CreateAccessTokenOnlyAsync(existing.User, ct);
            response.RefreshToken = newRefreshValue;
            return response;
        }

        public async Task RevokeAsync(string refreshToken, CancellationToken ct = default)
        {
            var tokenHash = HashToken(refreshToken);
            var existing = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);
            if (existing == null) return;
            existing.Revoked = true;
            await _db.SaveChangesAsync(ct);
        }

        private async Task<LoginResponse> CreateTokensForUserAsync(User user, CancellationToken ct = default)
        {
            var access = await CreateAccessTokenOnlyAsync(user, ct);
            var refreshValue = GenerateSecureToken();
            var refresh = new RefreshToken
            {
                Id = Guid.NewGuid(), // assign client-side so relations can reference it before SaveChanges
                UserId = user.Id,
                TokenHash = HashToken(refreshValue),
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(_jwt.RefreshTokenDays),
                Revoked = false
            };
            _db.RefreshTokens.Add(refresh);
            await _db.SaveChangesAsync(ct);

            access.RefreshToken = refreshValue;
            return access;
        }

        private async Task<LoginResponse> CreateAccessTokenOnlyAsync(User user, CancellationToken ct = default)
        {
            var roles = await _db.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .Select(ur => ur.Role.Name)
                .ToListAsync(ct);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email)
            };

            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var expiresUtc = DateTime.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);
            var expiresOffset = new DateTimeOffset(expiresUtc, TimeSpan.Zero);

            var token = new JwtSecurityToken(
                issuer: _jwt.Issuer,
                audience: _jwt.Audience,
                claims: claims,
                expires: expiresUtc,
                signingCredentials: creds);

            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenString = tokenHandler.WriteToken(token);

            return new LoginResponse
            {
                AccessToken = tokenString,
                ExpiresAt = expiresOffset
            };
        }

        private static string GenerateSecureToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes);
        }

        private static string HashToken(string token)
        {
            using var sha = SHA256.Create();
            var data = Encoding.UTF8.GetBytes(token);
            var hash = sha.ComputeHash(data);
            return Convert.ToHexString(hash);
        }
    }
}