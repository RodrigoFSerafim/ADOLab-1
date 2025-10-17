using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

/// <summary>
/// Representa uma entidade de matrícula de aluno em disciplina.
/// </summary>
public class Matricula
{
    /// <summary>
    /// Obtém ou define o ID da matrícula.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Obtém ou define o ID do aluno.
    /// </summary>
    [Required]
    public int AlunoId { get; set; }

    /// <summary>
    /// Obtém ou define o ID da disciplina.
    /// </summary>
    [Required]
    public int DisciplinaId { get; set; }

    /// <summary>
    /// Obtém ou define a data da matrícula.
    /// </summary>
    public DateTime DataMatricula { get; set; }

    /// <summary>
    /// Obtém ou define o status da matrícula.
    /// </summary>
    [MaxLength(20)]
    public string Status { get; set; } = "Ativa"; // Ativa, Concluída, Cancelada

    /// <summary>
    /// Obtém ou define a nota final do aluno na disciplina.
    /// </summary>
    public decimal? NotaFinal { get; set; }

    /// <summary>
    /// Obtém ou define a frequência do aluno na disciplina (em percentual).
    /// </summary>
    public decimal? Frequencia { get; set; }

    /// <summary>
    /// Navegação para o aluno matriculado.
    /// </summary>
    [ForeignKey("AlunoId")]
    public virtual Aluno Aluno { get; set; } = null!;

    /// <summary>
    /// Navegação para a disciplina da matrícula.
    /// </summary>
    [ForeignKey("DisciplinaId")]
    public virtual Disciplina Disciplina { get; set; } = null!;

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="Matricula"/>.
    /// </summary>
    public Matricula() { }

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="Matricula"/>.
    /// </summary>
    /// <param name="alunoId">O ID do aluno.</param>
    /// <param name="disciplinaId">O ID da disciplina.</param>
    /// <param name="dataMatricula">A data da matrícula.</param>
    /// <param name="status">O status da matrícula.</param>
    public Matricula(int alunoId, int disciplinaId, DateTime dataMatricula, string status = "Ativa")
    {
        AlunoId = alunoId;
        DisciplinaId = disciplinaId;
        DataMatricula = dataMatricula;
        Status = status;
    }
}
