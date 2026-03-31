using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BankMaskingAPI
{
    public sealed class JwtTokenPayload
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
    }

    public sealed class JwtTokenService
    {
        private readonly string _issuer;
        private readonly string _audience;
        private readonly string _key;
        private readonly int _expiresMinutes;

        public JwtTokenService(IConfiguration configuration)
        {
            _issuer = configuration["JwtSettings:Issuer"] ?? "BankMaskingAPI";
            _audience = configuration["JwtSettings:Audience"] ?? "BankMaskingClients";
            _key = configuration["JwtSettings:Key"] ?? "BANK_MASKING_DEMO_KEY_CHANGE_ME_2026";

            string expiresText = configuration["JwtSettings:ExpiresMinutes"] ?? "60";
            if (!int.TryParse(expiresText, out _expiresMinutes) || _expiresMinutes < 5)
            {
                _expiresMinutes = 60;
            }
        }

        public JwtTokenPayload CreateToken(string username, string role)
        {
            string normalizedUser = string.IsNullOrWhiteSpace(username) ? "unknown" : username.Trim().ToLowerInvariant();
            string normalizedRole = string.IsNullOrWhiteSpace(role) ? "cskh" : role.Trim().ToLowerInvariant();

            var now = DateTime.UtcNow;
            var expiresAt = now.AddMinutes(_expiresMinutes);
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, normalizedUser),
                new Claim(ClaimTypes.Name, normalizedUser),
                new Claim(ClaimTypes.Role, normalizedRole),
                new Claim("role", normalizedRole),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            };

            var jwt = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                notBefore: now,
                expires: expiresAt,
                signingCredentials: credentials);

            return new JwtTokenPayload
            {
                Token = new JwtSecurityTokenHandler().WriteToken(jwt),
                ExpiresAtUtc = expiresAt
            };
        }
    }
}
