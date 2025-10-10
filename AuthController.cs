using Microsoft.AspNetCore.Mvc;
using BCrypt.Net;
using System.Security.Claims;

/// <summary>
/// Controlador responsável pelas operações de autenticação.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly JwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="AuthController"/>.
    /// </summary>
    /// <param name="userRepository">O repositório de usuários.</param>
    /// <param name="jwtService">O serviço JWT.</param>
    /// <param name="logger">O logger.</param>
    public AuthController(IUserRepository userRepository, JwtService jwtService, ILogger<AuthController> logger)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _logger = logger;
    }

    /// <summary>
    /// Realiza o login do usuário.
    /// </summary>
    /// <param name="request">Os dados de login.</param>
    /// <returns>Um token JWT se o login for bem-sucedido.</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.UsernameOrEmail) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Nome de usuário/email e senha são obrigatórios." });
            }

            var user = _userRepository.BuscarPorUsernameOuEmail(request.UsernameOrEmail);
            if (user == null)
            {
                _logger.LogWarning($"Tentativa de login com usuário não encontrado: {request.UsernameOrEmail}");
                return Unauthorized(new { message = "Credenciais inválidas." });
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning($"Tentativa de login com senha incorreta para usuário: {user.Username}");
                return Unauthorized(new { message = "Credenciais inválidas." });
            }

            if (!user.IsActive)
            {
                _logger.LogWarning($"Tentativa de login com usuário inativo: {user.Username}");
                return Unauthorized(new { message = "Usuário inativo." });
            }

            var token = _jwtService.GenerateToken(user);
            var expiresAt = DateTime.UtcNow.AddMinutes(60); // Mesmo tempo do token

            _logger.LogInformation($"Login bem-sucedido para usuário: {user.Username}");

            return Ok(new AuthResponse
            {
                Token = token,
                ExpiresAt = expiresAt,
                User = new UserInfo
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante o processo de login");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Registra um novo usuário.
    /// </summary>
    /// <param name="request">Os dados de registro.</param>
    /// <returns>Informações do usuário criado.</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            // Validação dos dados de entrada
            if (string.IsNullOrWhiteSpace(request.Username) || 
                string.IsNullOrWhiteSpace(request.Email) || 
                string.IsNullOrWhiteSpace(request.Password) || 
                string.IsNullOrWhiteSpace(request.FullName))
            {
                return BadRequest(new { message = "Todos os campos são obrigatórios." });
            }

            if (request.Username.Length < 3)
            {
                return BadRequest(new { message = "Nome de usuário deve ter pelo menos 3 caracteres." });
            }

            if (request.Password.Length < 6)
            {
                return BadRequest(new { message = "Senha deve ter pelo menos 6 caracteres." });
            }

            if (!IsValidEmail(request.Email))
            {
                return BadRequest(new { message = "Email inválido." });
            }

            // Verificar se usuário já existe
            if (_userRepository.UsernameExiste(request.Username))
            {
                return BadRequest(new { message = "Nome de usuário já existe." });
            }

            if (_userRepository.EmailExiste(request.Email))
            {
                return BadRequest(new { message = "Email já está em uso." });
            }

            // Criar novo usuário
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var user = new User(request.Username, request.Email, passwordHash, request.FullName);

            var userId = _userRepository.Inserir(user);
            user.Id = userId;

            _logger.LogInformation($"Novo usuário registrado: {user.Username} (ID: {userId})");

            return CreatedAtAction(nameof(GetProfile), new { id = userId }, new UserInfo
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro durante o processo de registro");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Obtém o perfil do usuário autenticado.
    /// </summary>
    /// <returns>Informações do usuário autenticado.</returns>
    [HttpGet("profile")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Token inválido." });
            }

            var user = _userRepository.BuscarPorId(userId);
            if (user == null)
            {
                return NotFound(new { message = "Usuário não encontrado." });
            }

            return Ok(new UserInfo
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                CreatedAt = user.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter perfil do usuário");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Atualiza o perfil do usuário autenticado.
    /// </summary>
    /// <param name="request">Os novos dados do usuário.</param>
    /// <returns>Informações atualizadas do usuário.</returns>
    [HttpPut("profile")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(new { message = "Token inválido." });
            }

            var user = _userRepository.BuscarPorId(userId);
            if (user == null)
            {
                return NotFound(new { message = "Usuário não encontrado." });
            }

            // Atualizar apenas os campos fornecidos
            if (!string.IsNullOrWhiteSpace(request.FullName))
            {
                user.FullName = request.FullName;
            }

            if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
            {
                if (!IsValidEmail(request.Email))
                {
                    return BadRequest(new { message = "Email inválido." });
                }

                if (_userRepository.EmailExiste(request.Email))
                {
                    return BadRequest(new { message = "Email já está em uso." });
                }

                user.Email = request.Email;
            }

            if (!string.IsNullOrWhiteSpace(request.CurrentPassword) && !string.IsNullOrWhiteSpace(request.NewPassword))
            {
                if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                {
                    return BadRequest(new { message = "Senha atual incorreta." });
                }

                if (request.NewPassword.Length < 6)
                {
                    return BadRequest(new { message = "Nova senha deve ter pelo menos 6 caracteres." });
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            }

            var rowsAffected = _userRepository.Atualizar(user);
            if (rowsAffected > 0)
            {
                _logger.LogInformation($"Perfil atualizado para usuário: {user.Username}");
                return Ok(new UserInfo
                {
                    Id = user.Id,
                    Username = user.Username,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt
                });
            }

            return BadRequest(new { message = "Nenhuma alteração foi feita." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao atualizar perfil do usuário");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Valida se o token JWT é válido.
    /// </summary>
    /// <returns>Status da validação do token.</returns>
    [HttpGet("validate")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    public async Task<IActionResult> ValidateToken()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            var usernameClaim = User.FindFirst(ClaimTypes.Name);
            var roleClaim = User.FindFirst(ClaimTypes.Role);

            if (userIdClaim == null || usernameClaim == null)
            {
                return Unauthorized(new { message = "Token inválido." });
            }

            return Ok(new
            {
                message = "Token válido.",
                userId = userIdClaim.Value,
                username = usernameClaim.Value,
                role = roleClaim?.Value ?? "User"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao validar token");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Valida se um email é válido.
    /// </summary>
    /// <param name="email">O email a ser validado.</param>
    /// <returns>True se o email for válido; caso contrário, false.</returns>
    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Representa uma solicitação de atualização de perfil.
/// </summary>
public class UpdateProfileRequest
{
    /// <summary>
    /// Obtém ou define o nome completo.
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Obtém ou define o email.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Obtém ou define a senha atual.
    /// </summary>
    public string? CurrentPassword { get; set; }

    /// <summary>
    /// Obtém ou define a nova senha.
    /// </summary>
    public string? NewPassword { get; set; }
}
