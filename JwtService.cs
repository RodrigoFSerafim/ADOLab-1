using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

/// <summary>
/// Serviço responsável pela geração e validação de tokens JWT.
/// </summary>
public class JwtService
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="JwtService"/>.
    /// </summary>
    /// <param name="configuration">A configuração da aplicação.</param>
    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Gera um token JWT para o usuário especificado.
    /// </summary>
    /// <param name="user">O usuário para o qual gerar o token.</param>
    /// <returns>O token JWT gerado.</returns>
    public string GenerateToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey não encontrada.");
        var issuer = jwtSettings["Issuer"] ?? "ADOLab";
        var audience = jwtSettings["Audience"] ?? "ADOLabUsers";
        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("FullName", user.FullName),
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Valida um token JWT e retorna o principal do usuário.
    /// </summary>
    /// <param name="token">O token JWT a ser validado.</param>
    /// <returns>O principal do usuário se o token for válido; caso contrário, null.</returns>
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey não encontrada.");
            var issuer = jwtSettings["Issuer"] ?? "ADOLab";
            var audience = jwtSettings["Audience"] ?? "ADOLabUsers";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extrai o ID do usuário do token JWT.
    /// </summary>
    /// <param name="token">O token JWT.</param>
    /// <returns>O ID do usuário se encontrado; caso contrário, null.</returns>
    public int? GetUserIdFromToken(string token)
    {
        var principal = ValidateToken(token);
        if (principal?.Identity is ClaimsIdentity identity)
        {
            var userIdClaim = identity.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
        }
        return null;
    }

    /// <summary>
    /// Extrai o nome de usuário do token JWT.
    /// </summary>
    /// <param name="token">O token JWT.</param>
    /// <returns>O nome de usuário se encontrado; caso contrário, null.</returns>
    public string? GetUsernameFromToken(string token)
    {
        var principal = ValidateToken(token);
        if (principal?.Identity is ClaimsIdentity identity)
        {
            return identity.FindFirst(ClaimTypes.Name)?.Value;
        }
        return null;
    }

    /// <summary>
    /// Extrai o papel/role do usuário do token JWT.
    /// </summary>
    /// <param name="token">O token JWT.</param>
    /// <returns>O papel/role se encontrado; caso contrário, null.</returns>
    public string? GetRoleFromToken(string token)
    {
        var principal = ValidateToken(token);
        if (principal?.Identity is ClaimsIdentity identity)
        {
            return identity.FindFirst(ClaimTypes.Role)?.Value;
        }
        return null;
    }
}
