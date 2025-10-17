using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Representa uma entidade de estudante.
/// </summary>
public class Aluno
{
    /// <summary>
    /// Obtém ou define o ID do estudante.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Obtém ou define o nome do estudante.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a idade do estudante.
    /// </summary>
    public int Idade { get; set; }

    /// <summary>
    /// Obtém ou define o email do estudante.
    /// </summary>
    [Required]
    [MaxLength(100)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a data de nascimento do estudante.
    /// </summary>
    public DateTime DataNascimento { get; set; }

    /// <summary>
    /// Navegação para as matrículas do aluno.
    /// </summary>
    public virtual ICollection<Matricula> Matriculas { get; set; } = new List<Matricula>();

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="Aluno"/>.
    /// </summary>
    public Aluno() { }

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="Aluno"/>.
    /// </summary>
    /// <param name="id">O ID do estudante.</param>
    /// <param name="nome">O nome do estudante.</param>
    /// <param name="idade">A idade do estudante.</param>
    /// <param name="email">O email do estudante.</param>
    /// <param name="dataNascimento">A data de nascimento do estudante.</param>
    public Aluno(int id, string nome, int idade, string email, DateTime dataNascimento)
    {
        Id = id;
        Nome = nome;
        Idade = idade;
        Email = email;
        DataNascimento = dataNascimento;
    }
}