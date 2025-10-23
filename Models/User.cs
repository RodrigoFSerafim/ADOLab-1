/// <summary>
/// Representa uma entidade de usuário para autenticação.
/// </summary>
public class User
{
    /// <summary>
    /// Obtém ou define o ID do usuário.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Obtém ou define o nome de usuário.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define o email do usuário.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a senha criptografada do usuário.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define o nome completo do usuário.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a data de criação do usuário.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Obtém ou define se o usuário está ativo.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Obtém ou define o papel/role do usuário.
    /// </summary>
    public string Role { get; set; } = "User";

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="User"/>.
    /// </summary>
    public User()
    {
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="User"/>.
    /// </summary>
    /// <param name="username">O nome de usuário.</param>
    /// <param name="email">O email do usuário.</param>
    /// <param name="passwordHash">A senha criptografada.</param>
    /// <param name="fullName">O nome completo.</param>
    /// <param name="role">O papel/role do usuário.</param>
    public User(string username, string email, string passwordHash, string fullName, string role = "User")
    {
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        FullName = fullName;
        Role = role;
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
    }
}

/// <summary>
/// Representa uma solicitação de login.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Obtém ou define o nome de usuário ou email.
    /// </summary>
    public string UsernameOrEmail { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a senha.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Representa uma solicitação de registro de usuário.
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// Obtém ou define o nome de usuário.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define o email.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a senha.
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define o nome completo.
    /// </summary>
    public string FullName { get; set; } = string.Empty;
}

/// <summary>
/// Representa uma resposta de autenticação com token.
/// </summary>
public class AuthResponse
{
    /// <summary>
    /// Obtém ou define o token JWT.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a data de expiração do token.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Obtém ou define as informações do usuário.
    /// </summary>
    public UserInfo? User { get; set; }
}

/// <summary>
/// Representa informações básicas do usuário (sem senha).
/// </summary>
public class UserInfo
{
    /// <summary>
    /// Obtém ou define o ID do usuário.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Obtém ou define o nome de usuário.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define o email do usuário.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define o nome completo do usuário.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define o papel/role do usuário.
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a data de criação do usuário.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
