using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Representa uma entidade de professor.
/// </summary>
public class Professor
{
    /// <summary>
    /// Obtém ou define o ID do professor.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Obtém ou define o nome do professor.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define o email do professor.
    /// </summary>
    [Required]
    [MaxLength(100)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a especialidade do professor.
    /// </summary>
    [MaxLength(100)]
    public string? Especialidade { get; set; }

    /// <summary>
    /// Obtém ou define a data de contratação do professor.
    /// </summary>
    public DateTime DataContratacao { get; set; }

    /// <summary>
    /// Obtém ou define se o professor está ativo.
    /// </summary>
    public bool Ativo { get; set; } = true;

    /// <summary>
    /// Navegação para as disciplinas ministradas pelo professor.
    /// </summary>
    public virtual ICollection<Disciplina> Disciplinas { get; set; } = new List<Disciplina>();

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="Professor"/>.
    /// </summary>
    public Professor() { }

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="Professor"/>.
    /// </summary>
    /// <param name="nome">O nome do professor.</param>
    /// <param name="email">O email do professor.</param>
    /// <param name="especialidade">A especialidade do professor.</param>
    /// <param name="dataContratacao">A data de contratação do professor.</param>
    public Professor(string nome, string email, string? especialidade, DateTime dataContratacao)
    {
        Nome = nome;
        Email = email;
        Especialidade = especialidade;
        DataContratacao = dataContratacao;
        Ativo = true;
    }
}
