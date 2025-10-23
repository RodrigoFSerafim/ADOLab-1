using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Representa uma entidade de disciplina.
/// </summary>
public class Disciplina
{
    /// <summary>
    /// Obtém ou define o ID da disciplina.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Obtém ou define o nome da disciplina.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define o código da disciplina.
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a carga horária da disciplina.
    /// </summary>
    public int CargaHoraria { get; set; }

    /// <summary>
    /// Obtém ou define a descrição da disciplina.
    /// </summary>
    [MaxLength(500)]
    public string? Descricao { get; set; }

    /// <summary>
    /// Obtém ou define o ID do professor responsável pela disciplina.
    /// </summary>
    [Required]
    public int ProfessorId { get; set; }

    /// <summary>
    /// Obtém ou define se a disciplina está ativa.
    /// </summary>
    public bool Ativa { get; set; } = true;

    /// <summary>
    /// Navegação para o professor responsável pela disciplina.
    /// </summary>
    [ForeignKey("ProfessorId")]
    public virtual Professor Professor { get; set; } = null!;

    /// <summary>
    /// Navegação para as matrículas na disciplina.
    /// </summary>
    public virtual ICollection<Matricula> Matriculas { get; set; } = new List<Matricula>();

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="Disciplina"/>.
    /// </summary>
    public Disciplina() { }

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="Disciplina"/>.
    /// </summary>
    /// <param name="nome">O nome da disciplina.</param>
    /// <param name="codigo">O código da disciplina.</param>
    /// <param name="cargaHoraria">A carga horária da disciplina.</param>
    /// <param name="descricao">A descrição da disciplina.</param>
    /// <param name="professorId">O ID do professor responsável.</param>
    public Disciplina(string nome, string codigo, int cargaHoraria, string? descricao, int professorId)
    {
        Nome = nome;
        Codigo = codigo;
        CargaHoraria = cargaHoraria;
        Descricao = descricao;
        ProfessorId = professorId;
        Ativa = true;
    }
}
