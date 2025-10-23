using Microsoft.EntityFrameworkCore;
using ADOLabDbContext = ADOLabDbContext;

/// <summary>
/// Serviço para lógica de negócio relacionada a matrículas.
/// </summary>
public class MatriculaService
{
    private readonly ADOLabDbContext _context;
    private readonly ILogger<MatriculaService> _logger;

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="MatriculaService"/>.
    /// </summary>
    /// <param name="context">O contexto do banco de dados.</param>
    /// <param name="logger">O logger.</param>
    public MatriculaService(ADOLabDbContext context, ILogger<MatriculaService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Calcula a situação de uma matrícula baseada na nota e frequência.
    /// </summary>
    /// <param name="notaFinal">A nota final.</param>
    /// <param name="frequencia">A frequência em percentual.</param>
    /// <returns>O status da matrícula.</returns>
    public string CalcularSituacao(decimal notaFinal, decimal frequencia)
    {
        if (notaFinal >= 6 && frequencia >= 75)
            return "Concluída";
        else if (notaFinal < 6 || frequencia < 75)
            return "Reprovada";
        else
            return "Ativa";
    }

    /// <summary>
    /// Verifica se um aluno pode se matricular em uma disciplina.
    /// </summary>
    /// <param name="alunoId">O ID do aluno.</param>
    /// <param name="disciplinaId">O ID da disciplina.</param>
    /// <returns>True se pode se matricular, false caso contrário.</returns>
    public async Task<bool> PodeSeMatricular(int alunoId, int disciplinaId)
    {
        try
        {
            // Verificar se já está matriculado
            var jaMatriculado = await _context.Matriculas
                .AnyAsync(m => m.AlunoId == alunoId && m.DisciplinaId == disciplinaId);

            if (jaMatriculado)
                return false;

            // Verificar se a disciplina está ativa
            var disciplinaAtiva = await _context.Disciplinas
                .AnyAsync(d => d.Id == disciplinaId && d.Ativa);

            if (!disciplinaAtiva)
                return false;

            // Verificar se o aluno existe
            var alunoExiste = await _context.Alunos
                .AnyAsync(a => a.Id == alunoId);

            if (!alunoExiste)
                return false;

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao verificar se aluno pode se matricular. AlunoId: {alunoId}, DisciplinaId: {disciplinaId}");
            return false;
        }
    }

    /// <summary>
    /// Obtém estatísticas de matrículas por período.
    /// </summary>
    /// <param name="dataInicio">A data de início do período.</param>
    /// <param name="dataFim">A data de fim do período.</param>
    /// <returns>Estatísticas de matrículas.</returns>
    public async Task<MatriculaEstatisticas> ObterEstatisticasPeriodo(DateTime dataInicio, DateTime dataFim)
    {
        try
        {
            var matriculas = await _context.Matriculas
                .Include(m => m.Aluno)
                .Include(m => m.Disciplina)
                .Where(m => m.DataMatricula >= dataInicio && m.DataMatricula <= dataFim)
                .ToListAsync();

            var totalMatriculas = matriculas.Count;
            var matriculasAtivas = matriculas.Count(m => m.Status == "Ativa");
            var matriculasConcluidas = matriculas.Count(m => m.Status == "Concluída");
            var matriculasReprovadas = matriculas.Count(m => m.Status == "Reprovada");

            var mediaNotas = matriculas
                .Where(m => m.NotaFinal.HasValue)
                .Average(m => m.NotaFinal!.Value);

            var mediaFrequencia = matriculas
                .Where(m => m.Frequencia.HasValue)
                .Average(m => m.Frequencia!.Value);

            return new MatriculaEstatisticas
            {
                DataInicio = dataInicio,
                DataFim = dataFim,
                TotalMatriculas = totalMatriculas,
                MatriculasAtivas = matriculasAtivas,
                MatriculasConcluidas = matriculasConcluidas,
                MatriculasReprovadas = matriculasReprovadas,
                MediaNotas = mediaNotas,
                MediaFrequencia = mediaFrequencia
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao obter estatísticas de matrículas. Período: {dataInicio:yyyy-MM-dd} a {dataFim:yyyy-MM-dd}");
            return new MatriculaEstatisticas
            {
                DataInicio = dataInicio,
                DataFim = dataFim
            };
        }
    }

    /// <summary>
    /// Obtém disciplinas mais procuradas.
    /// </summary>
    /// <param name="limite">O número máximo de disciplinas a retornar.</param>
    /// <returns>Lista de disciplinas mais procuradas.</returns>
    public async Task<List<DisciplinaPopularidade>> ObterDisciplinasMaisProcuradas(int limite = 10)
    {
        try
        {
            var disciplinas = await _context.Disciplinas
                .Include(d => d.Matriculas)
                .Where(d => d.Ativa)
                .Select(d => new DisciplinaPopularidade
                {
                    DisciplinaId = d.Id,
                    Nome = d.Nome,
                    Codigo = d.Codigo,
                    Professor = d.Professor.Nome,
                    TotalMatriculas = d.Matriculas.Count,
                    MatriculasAtivas = d.Matriculas.Count(m => m.Status == "Ativa"),
                    MatriculasConcluidas = d.Matriculas.Count(m => m.Status == "Concluída")
                })
                .OrderByDescending(d => d.TotalMatriculas)
                .Take(limite)
                .ToListAsync();

            return disciplinas;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter disciplinas mais procuradas");
            return new List<DisciplinaPopularidade>();
        }
    }

    /// <summary>
    /// Obtém alunos com melhor desempenho em uma disciplina.
    /// </summary>
    /// <param name="disciplinaId">O ID da disciplina.</param>
    /// <param name="limite">O número máximo de alunos a retornar.</param>
    /// <returns>Lista de alunos com melhor desempenho.</returns>
    public async Task<List<AlunoDesempenhoDisciplina>> ObterMelhoresAlunosDisciplina(int disciplinaId, int limite = 10)
    {
        try
        {
            var alunos = await _context.Matriculas
                .Include(m => m.Aluno)
                .Include(m => m.Disciplina)
                .Where(m => m.DisciplinaId == disciplinaId && m.NotaFinal.HasValue)
                .Select(m => new AlunoDesempenhoDisciplina
                {
                    AlunoId = m.AlunoId,
                    NomeAluno = m.Aluno.Nome,
                    EmailAluno = m.Aluno.Email,
                    NomeDisciplina = m.Disciplina.Nome,
                    NotaFinal = m.NotaFinal!.Value,
                    Frequencia = m.Frequencia ?? 0,
                    Status = m.Status
                })
                .OrderByDescending(a => a.NotaFinal)
                .Take(limite)
                .ToListAsync();

            return alunos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao obter melhores alunos da disciplina. DisciplinaId: {disciplinaId}");
            return new List<AlunoDesempenhoDisciplina>();
        }
    }
}

/// <summary>
/// Representa estatísticas de matrículas.
/// </summary>
public class MatriculaEstatisticas
{
    /// <summary>
    /// Obtém ou define a data de início do período.
    /// </summary>
    public DateTime DataInicio { get; set; }

    /// <summary>
    /// Obtém ou define a data de fim do período.
    /// </summary>
    public DateTime DataFim { get; set; }

    /// <summary>
    /// Obtém ou define o total de matrículas.
    /// </summary>
    public int TotalMatriculas { get; set; }

    /// <summary>
    /// Obtém ou define o número de matrículas ativas.
    /// </summary>
    public int MatriculasAtivas { get; set; }

    /// <summary>
    /// Obtém ou define o número de matrículas concluídas.
    /// </summary>
    public int MatriculasConcluidas { get; set; }

    /// <summary>
    /// Obtém ou define o número de matrículas reprovadas.
    /// </summary>
    public int MatriculasReprovadas { get; set; }

    /// <summary>
    /// Obtém ou define a média das notas.
    /// </summary>
    public double MediaNotas { get; set; }

    /// <summary>
    /// Obtém ou define a média da frequência.
    /// </summary>
    public double MediaFrequencia { get; set; }
}

/// <summary>
/// Representa a popularidade de uma disciplina.
/// </summary>
public class DisciplinaPopularidade
{
    /// <summary>
    /// Obtém ou define o ID da disciplina.
    /// </summary>
    public int DisciplinaId { get; set; }

    /// <summary>
    /// Obtém ou define o nome da disciplina.
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define o código da disciplina.
    /// </summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define o nome do professor.
    /// </summary>
    public string Professor { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define o total de matrículas.
    /// </summary>
    public int TotalMatriculas { get; set; }

    /// <summary>
    /// Obtém ou define o número de matrículas ativas.
    /// </summary>
    public int MatriculasAtivas { get; set; }

    /// <summary>
    /// Obtém ou define o número de matrículas concluídas.
    /// </summary>
    public int MatriculasConcluidas { get; set; }
}

/// <summary>
/// Representa o desempenho de um aluno em uma disciplina.
/// </summary>
public class AlunoDesempenhoDisciplina
{
    /// <summary>
    /// Obtém ou define o ID do aluno.
    /// </summary>
    public int AlunoId { get; set; }

    /// <summary>
    /// Obtém ou define o nome do aluno.
    /// </summary>
    public string NomeAluno { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define o email do aluno.
    /// </summary>
    public string EmailAluno { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define o nome da disciplina.
    /// </summary>
    public string NomeDisciplina { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a nota final.
    /// </summary>
    public decimal NotaFinal { get; set; }

    /// <summary>
    /// Obtém ou define a frequência.
    /// </summary>
    public decimal Frequencia { get; set; }

    /// <summary>
    /// Obtém ou define o status da matrícula.
    /// </summary>
    public string Status { get; set; } = string.Empty;
}
