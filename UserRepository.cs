using System.Data;
using Microsoft.Data.SqlClient;
using BCrypt.Net;

/// <summary>
/// Interface para repositório de usuários.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Garante que o esquema do banco de dados para a tabela Users exista.
    /// </summary>
    void GarantirEsquema();

    /// <summary>
    /// Insere um novo usuário no banco de dados.
    /// </summary>
    /// <param name="user">O usuário a ser inserido.</param>
    /// <returns>O ID do usuário recém-inserido.</returns>
    int Inserir(User user);

    /// <summary>
    /// Busca um usuário por nome de usuário ou email.
    /// </summary>
    /// <param name="usernameOrEmail">O nome de usuário ou email.</param>
    /// <returns>O usuário encontrado ou null se não encontrado.</returns>
    User? BuscarPorUsernameOuEmail(string usernameOrEmail);

    /// <summary>
    /// Busca um usuário por ID.
    /// </summary>
    /// <param name="id">O ID do usuário.</param>
    /// <returns>O usuário encontrado ou null se não encontrado.</returns>
    User? BuscarPorId(int id);

    /// <summary>
    /// Verifica se um nome de usuário já existe.
    /// </summary>
    /// <param name="username">O nome de usuário.</param>
    /// <returns>True se o nome de usuário já existe; caso contrário, false.</returns>
    bool UsernameExiste(string username);

    /// <summary>
    /// Verifica se um email já existe.
    /// </summary>
    /// <param name="email">O email.</param>
    /// <returns>True se o email já existe; caso contrário, false.</returns>
    bool EmailExiste(string email);

    /// <summary>
    /// Lista todos os usuários.
    /// </summary>
    /// <returns>Uma lista de usuários.</returns>
    List<User> Listar();

    /// <summary>
    /// Atualiza um usuário no banco de dados.
    /// </summary>
    /// <param name="user">O usuário a ser atualizado.</param>
    /// <returns>O número de linhas afetadas.</returns>
    int Atualizar(User user);

    /// <summary>
    /// Exclui um usuário do banco de dados.
    /// </summary>
    /// <param name="id">O ID do usuário a ser excluído.</param>
    /// <returns>O número de linhas afetadas.</returns>
    int Excluir(int id);
}

/// <summary>
/// Classe de repositório para gerenciar entidades User no banco de dados.
/// </summary>
public class UserRepository : IUserRepository
{
    /// <summary>
    /// Obtém ou define a string de conexão com o banco de dados.
    /// </summary>
    public string ConnectionString { get; set; }

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="UserRepository"/>.
    /// </summary>
    /// <param name="connectionString">A string de conexão com o banco de dados.</param>
    public UserRepository(string connectionString)
    {
        ConnectionString = connectionString;
    }

    /// <summary>
    /// Garante que o esquema do banco de dados para a tabela Users exista.
    /// </summary>
    public void GarantirEsquema()
    {
        const string ddl = @"
        IF OBJECT_ID('dbo.Users', 'U') IS NULL
        BEGIN
            CREATE TABLE dbo.Users (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                Username NVARCHAR(50) NOT NULL UNIQUE,
                Email NVARCHAR(100) NOT NULL UNIQUE,
                PasswordHash NVARCHAR(255) NOT NULL,
                FullName NVARCHAR(100) NOT NULL,
                CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
                IsActive BIT NOT NULL DEFAULT 1,
                Role NVARCHAR(20) NOT NULL DEFAULT 'User'
            );
            
            -- Criar índices para melhor performance
            CREATE INDEX IX_Users_Username ON dbo.Users(Username);
            CREATE INDEX IX_Users_Email ON dbo.Users(Email);
        END";
        
        using var conn = new SqlConnection(ConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(ddl, conn) { CommandType = CommandType.Text, CommandTimeout = 30 };
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Insere um novo usuário no banco de dados.
    /// </summary>
    /// <param name="user">O usuário a ser inserido.</param>
    /// <returns>O ID do usuário recém-inserido.</returns>
    public int Inserir(User user)
    {
        const string sql = @"
            INSERT INTO dbo.Users (Username, Email, PasswordHash, FullName, CreatedAt, IsActive, Role)
            VALUES (@Username, @Email, @PasswordHash, @FullName, @CreatedAt, @IsActive, @Role);
            SELECT SCOPE_IDENTITY();";

        using var conn = new SqlConnection(ConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn) { CommandType = CommandType.Text, CommandTimeout = 30 };
        
        cmd.Parameters.AddWithValue("@Username", user.Username);
        cmd.Parameters.AddWithValue("@Email", user.Email);
        cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
        cmd.Parameters.AddWithValue("@FullName", user.FullName);
        cmd.Parameters.AddWithValue("@CreatedAt", user.CreatedAt);
        cmd.Parameters.AddWithValue("@IsActive", user.IsActive);
        cmd.Parameters.AddWithValue("@Role", user.Role);

        var result = cmd.ExecuteScalar();
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// Busca um usuário por nome de usuário ou email.
    /// </summary>
    /// <param name="usernameOrEmail">O nome de usuário ou email.</param>
    /// <returns>O usuário encontrado ou null se não encontrado.</returns>
    public User? BuscarPorUsernameOuEmail(string usernameOrEmail)
    {
        const string sql = @"
            SELECT Id, Username, Email, PasswordHash, FullName, CreatedAt, IsActive, Role
            FROM dbo.Users 
            WHERE (Username = @UsernameOrEmail OR Email = @UsernameOrEmail) AND IsActive = 1";

        using var conn = new SqlConnection(ConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn) { CommandType = CommandType.Text, CommandTimeout = 30 };
        cmd.Parameters.AddWithValue("@UsernameOrEmail", usernameOrEmail);
        using var reader = cmd.ExecuteReader();

        if (reader.Read())
        {
            return new User
            {
                Id = reader.GetInt32("Id"),
                Username = reader.GetString("Username"),
                Email = reader.GetString("Email"),
                PasswordHash = reader.GetString("PasswordHash"),
                FullName = reader.GetString("FullName"),
                CreatedAt = reader.GetDateTime("CreatedAt"),
                IsActive = reader.GetBoolean("IsActive"),
                Role = reader.GetString("Role")
            };
        }

        return null;
    }

    /// <summary>
    /// Busca um usuário por ID.
    /// </summary>
    /// <param name="id">O ID do usuário.</param>
    /// <returns>O usuário encontrado ou null se não encontrado.</returns>
    public User? BuscarPorId(int id)
    {
        const string sql = @"
            SELECT Id, Username, Email, PasswordHash, FullName, CreatedAt, IsActive, Role
            FROM dbo.Users 
            WHERE Id = @Id AND IsActive = 1";

        using var conn = new SqlConnection(ConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn) { CommandType = CommandType.Text, CommandTimeout = 30 };
        cmd.Parameters.AddWithValue("@Id", id);
        using var reader = cmd.ExecuteReader();

        if (reader.Read())
        {
            return new User
            {
                Id = reader.GetInt32("Id"),
                Username = reader.GetString("Username"),
                Email = reader.GetString("Email"),
                PasswordHash = reader.GetString("PasswordHash"),
                FullName = reader.GetString("FullName"),
                CreatedAt = reader.GetDateTime("CreatedAt"),
                IsActive = reader.GetBoolean("IsActive"),
                Role = reader.GetString("Role")
            };
        }

        return null;
    }

    /// <summary>
    /// Verifica se um nome de usuário já existe.
    /// </summary>
    /// <param name="username">O nome de usuário.</param>
    /// <returns>True se o nome de usuário já existe; caso contrário, false.</returns>
    public bool UsernameExiste(string username)
    {
        const string sql = "SELECT COUNT(1) FROM dbo.Users WHERE Username = @Username";

        using var conn = new SqlConnection(ConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn) { CommandType = CommandType.Text, CommandTimeout = 30 };
        cmd.Parameters.AddWithValue("@Username", username);

        var count = Convert.ToInt32(cmd.ExecuteScalar());
        return count > 0;
    }

    /// <summary>
    /// Verifica se um email já existe.
    /// </summary>
    /// <param name="email">O email.</param>
    /// <returns>True se o email já existe; caso contrário, false.</returns>
    public bool EmailExiste(string email)
    {
        const string sql = "SELECT COUNT(1) FROM dbo.Users WHERE Email = @Email";

        using var conn = new SqlConnection(ConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn) { CommandType = CommandType.Text, CommandTimeout = 30 };
        cmd.Parameters.AddWithValue("@Email", email);

        var count = Convert.ToInt32(cmd.ExecuteScalar());
        return count > 0;
    }

    /// <summary>
    /// Lista todos os usuários.
    /// </summary>
    /// <returns>Uma lista de usuários.</returns>
    public List<User> Listar()
    {
        const string sql = @"
            SELECT Id, Username, Email, PasswordHash, FullName, CreatedAt, IsActive, Role
            FROM dbo.Users 
            WHERE IsActive = 1
            ORDER BY CreatedAt DESC";
        
        var users = new List<User>();

        using var conn = new SqlConnection(ConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn) { CommandType = CommandType.Text, CommandTimeout = 30 };
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var user = new User
            {
                Id = reader.GetInt32("Id"),
                Username = reader.GetString("Username"),
                Email = reader.GetString("Email"),
                PasswordHash = reader.GetString("PasswordHash"),
                FullName = reader.GetString("FullName"),
                CreatedAt = reader.GetDateTime("CreatedAt"),
                IsActive = reader.GetBoolean("IsActive"),
                Role = reader.GetString("Role")
            };
            users.Add(user);
        }

        return users;
    }

    /// <summary>
    /// Atualiza um usuário no banco de dados.
    /// </summary>
    /// <param name="user">O usuário a ser atualizado.</param>
    /// <returns>O número de linhas afetadas.</returns>
    public int Atualizar(User user)
    {
        const string sql = @"
            UPDATE dbo.Users 
            SET Username = @Username, Email = @Email, PasswordHash = @PasswordHash, 
                FullName = @FullName, IsActive = @IsActive, Role = @Role
            WHERE Id = @Id";

        using var conn = new SqlConnection(ConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn) { CommandType = CommandType.Text, CommandTimeout = 30 };
        
        cmd.Parameters.AddWithValue("@Id", user.Id);
        cmd.Parameters.AddWithValue("@Username", user.Username);
        cmd.Parameters.AddWithValue("@Email", user.Email);
        cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
        cmd.Parameters.AddWithValue("@FullName", user.FullName);
        cmd.Parameters.AddWithValue("@IsActive", user.IsActive);
        cmd.Parameters.AddWithValue("@Role", user.Role);

        return cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Exclui um usuário do banco de dados.
    /// </summary>
    /// <param name="id">O ID do usuário a ser excluído.</param>
    /// <returns>O número de linhas afetadas.</returns>
    public int Excluir(int id)
    {
        const string sql = "DELETE FROM dbo.Users WHERE Id = @Id";

        using var conn = new SqlConnection(ConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn) { CommandType = CommandType.Text, CommandTimeout = 30 };
        cmd.Parameters.AddWithValue("@Id", id);

        return cmd.ExecuteNonQuery();
    }
}
