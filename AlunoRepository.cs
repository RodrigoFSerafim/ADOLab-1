using System.Data;
using Microsoft.Data.SqlClient;

/// <summary>
/// Classe de reposit�rio para gerenciar entidades Aluno no banco de dados.
/// </summary>
public class AlunoRepository : IRepository<Aluno>
{
    /// <summary>
    /// Obt�m ou define a string de conex�o com o banco de dados.
    /// </summary>
    public string ConnectionString { get; set; }

    /// <summary>
    /// Inicializa uma nova inst�ncia da classe <see cref="AlunoRepository"/>.
    /// </summary>
    /// <param name="connectionString">A string de conex�o com o banco de dados.</param>
    public AlunoRepository(string connectionString)
    {
        ConnectionString = connectionString;
    }

    /// <summary>
    /// Garante que o esquema do banco de dados para a tabela Aluno exista.
    /// </summary>
    public void GarantirEsquema()
    {
        const string ddl = @"
        IF OBJECT_ID('dbo.Alunos', 'U') IS NULL
        BEGIN
            CREATE TABLE dbo.Alunos (
                Id INT IDENTITY(1,1) PRIMARY KEY,
                Nome NVARCHAR(100) NOT NULL,
                Idade INT NOT NULL,
                Email NVARCHAR(100) NOT NULL,
                DataNascimento DATE NOT NULL
            );
        END";
        using var conn = new SqlConnection(ConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(ddl, conn) { CommandType = CommandType.Text, CommandTimeout = 30 };
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Insere um novo registro de Aluno no banco de dados.
    /// </summary>
    /// <param name="nome">O nome do Aluno.</param>
    /// <param name="idade">A idade do Aluno.</param>
    /// <param name="email">O email do Aluno.</param>
    /// <param name="dataNascimento">A data de nascimento do Aluno.</param>
    /// <returns>O ID do Aluno rec�m-inserido.</returns>
    public int Inserir(string nome, int idade, string email, DateTime dataNascimento)
    {
        const string sql = @"
            INSERT INTO dbo.Alunos (Nome, Idade, Email, DataNascimento)
            VALUES (@Nome, @Idade, @Email, @DataNascimento);
            SELECT SCOPE_IDENTITY();";

        using var conn = new SqlConnection(ConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn) { CommandType = CommandType.Text, CommandTimeout = 30 };
        
        cmd.Parameters.AddWithValue("@Nome", nome);
        cmd.Parameters.AddWithValue("@Idade", idade);
        cmd.Parameters.AddWithValue("@Email", email);
        cmd.Parameters.AddWithValue("@DataNascimento", dataNascimento);

        var result = cmd.ExecuteScalar();
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// Recupera uma lista de todos os registros de Aluno do banco de dados.
    /// </summary>
    /// <returns>Uma lista de entidades Aluno.</returns>
    public List<Aluno> Listar()
    {
        const string sql = "SELECT Id, Nome, Idade, Email, DataNascimento FROM dbo.Alunos ORDER BY Id";
        var alunos = new List<Aluno>();

        using var conn = new SqlConnection(ConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn) { CommandType = CommandType.Text, CommandTimeout = 30 };
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var aluno = new Aluno(
                reader.GetInt32("Id"),
                reader.GetString("Nome"),
                reader.GetInt32("Idade"),
                reader.GetString("Email"),
                reader.GetDateTime("DataNascimento")
            );
            alunos.Add(aluno);
        }

        return alunos;
    }

    /// <summary>
    /// Atualiza os dados de um registro de Aluno no banco de dados.
    /// </summary>
    /// <param name="id">O ID do Aluno a ser atualizado.</param>
    /// <param name="nome">O novo nome do Aluno.</param>
    /// <param name="idade">A nova idade do Aluno.</param>
    /// <param name="email">O novo email do Aluno.</param>
    /// <param name="dataNascimento">A nova data de nascimento do Aluno.</param>
    /// <returns>O n�mero de linhas afetadas.</returns>
    public int Atualizar(int id, string nome, int idade, string email, DateTime dataNascimento)
    {
        const string sql = @"
            UPDATE dbo.Alunos 
            SET Nome = @Nome, Idade = @Idade, Email = @Email, DataNascimento = @DataNascimento
            WHERE Id = @Id";

        using var conn = new SqlConnection(ConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn) { CommandType = CommandType.Text, CommandTimeout = 30 };
        
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@Nome", nome);
        cmd.Parameters.AddWithValue("@Idade", idade);
        cmd.Parameters.AddWithValue("@Email", email);
        cmd.Parameters.AddWithValue("@DataNascimento", dataNascimento);

        return cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Exclui um registro de Aluno do banco de dados.
    /// </summary>
    /// <param name="id">O ID do Aluno a ser exclu�do.</param>
    /// <returns>O n�mero de linhas afetadas.</returns>
    public int Excluir(int id)
    {
        const string sql = "DELETE FROM dbo.Alunos WHERE Id = @Id";

        using var conn = new SqlConnection(ConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn) { CommandType = CommandType.Text, CommandTimeout = 30 };
        
        cmd.Parameters.AddWithValue("@Id", id);

        return cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Busca registros de Aluno no banco de dados com base em um termo e valor.
    /// </summary>
    /// <param name="propriedade">A propriedade a ser pesquisada (coluna).</param>
    /// <param name="valor">O valor a ser pesquisado.</param>
    /// <returns>Uma lista de entidades Aluno correspondentes.</returns>
    public List<Aluno> Buscar(string propriedade, object valor)
    {
        // Validação das propriedades permitidas para evitar SQL injection
        var propriedadesValidas = new[] { "Nome", "Idade", "Email", "DataNascimento" };
        if (!propriedadesValidas.Contains(propriedade))
        {
            throw new ArgumentException($"Propriedade '{propriedade}' não é válida para busca.");
        }

        var sql = $"SELECT Id, Nome, Idade, Email, DataNascimento FROM dbo.Alunos WHERE {propriedade} = @Valor ORDER BY Id";
        var alunos = new List<Aluno>();

        using var conn = new SqlConnection(ConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn) { CommandType = CommandType.Text, CommandTimeout = 30 };
        
        cmd.Parameters.AddWithValue("@Valor", valor ?? DBNull.Value);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var aluno = new Aluno(
                reader.GetInt32("Id"),
                reader.GetString("Nome"),
                reader.GetInt32("Idade"),
                reader.GetString("Email"),
                reader.GetDateTime("DataNascimento")
            );
            alunos.Add(aluno);
        }

        return alunos;
    }
}