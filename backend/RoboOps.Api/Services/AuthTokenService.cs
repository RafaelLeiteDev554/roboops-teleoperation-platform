using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using RoboOps.Api.Models;

namespace RoboOps.Api.Services;

public sealed class AuthTokenService(IConfiguration configuration)
{
    private static readonly IReadOnlyDictionary<string, string> DemoUsers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["admin"] = "Admin",
        ["operator"] = "Operator",
        ["reviewer"] = "Reviewer",
        ["data"] = "DataEngineer"
    };

    public LoginResponse? Login(LoginRequest request)
    {
        if (!DemoUsers.TryGetValue(request.Username, out var role) || request.Password != "roboops")
        {
            return null;
        }

        var expiresAt = DateTimeOffset.UtcNow.AddHours(8);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, request.Username),
            new Claim(ClaimTypes.Name, request.Username),
            new Claim(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetSigningKey()));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "RoboOps",
            audience: "RoboOps.OperatorConsole",
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), role, expiresAt);
    }

    public string GetSigningKey() =>
        configuration["Auth:SigningKey"] ?? "roboops-local-development-signing-key-change-me";
}
