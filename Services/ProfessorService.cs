using Microsoft.EntityFrameworkCore;
using ADOLabDbContext = ADOLabDbContext;

/// <summary>
/// Serviço para lógica de negócio relacionada a professores.
/// </summary>
public class ProfessorService
{
    private readonly ADOLabDbContext _context;
    private readonly ILogger<ProfessorService> _logger;

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="ProfessorService"/>.
    /// </summary>
    /// <param name="context">O contexto do banco de dados.</param>
    /// <param name="logger">O logger.</param>
    public ProfessorService(ADOLabDbContext context, ILogger<ProfessorService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Calcula o tempo de experiência de um professor.
    /// </summary>
    /// <param name="dataContratacao">A data de contratação.</param>
    /// <returns>O tempo de experiência em anos.</returns>
    public int CalcularTempoExperiencia(DateTime dataContratacao)
    {
        var hoje = DateTime.Today;
        var anos = hoje.Year - dataContratacao.Year;
        
        if (dataContratacao.Date > hoje.AddYears(-anos))
            anos--;

        return anos;
    }

    /// <summary>
    /// Obtém estatísticas de um professor.
    /// </summary>
    /// <param name="professorId">O ID do professor.</param>
    /// <returns>Estatísticas do professor.</returns>
    public async Task<ProfessorEstatisticas?> ObterEstatisticas(int professorId)
    {
        try
        {
            var professor = await _context.Professores
                .Include(p => p.Disciplinas)
                    .ThenInclude(d => d.Matriculas)
                .FirstOrDefaultAsync(p => p.Id == professorId && p.Ativo);

            if (professor == null)
                return null;

            var disciplinas = professor.Disciplinas.Where(d => d.Ativa);
            var totalDisciplinas = disciplinas.Count();
            var totalMatriculas = disciplinas.Sum(d => d.Matriculas.Count);
            var matriculasAtivas = disciplinas.Sum(d => d.Matriculas.Count(m => m.Status == "Ativa"));
            var matriculasConcluidas = disciplinas.Sum(d => d.Matriculas.Count(m => m.Status == "Concluída"));

            var tempoExperiencia = CalcularTempoExperiencia(professor.DataContratacao);

            return new ProfessorEstatisticas
            {
                ProfessorId = professorId,
                Nome = professor.Nome,
                Especialidade = professor.Especialidade,
                TempoExperiencia = tempoExperiencia,
                TotalDisciplinas = totalDisciplinas,
                TotalMatriculas = totalMatriculas,
                MatriculasAtivas = matriculasAtivas,
                MatriculasConcluidas = matriculasConcluidas
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao obter estatísticas do professor. ID: {professorId}");
            return null;
        }
    }

    /// <summary>
    /// Obtém professores por especialidade.
    /// </summary>
    /// <param name="especialidade">A especialidade a ser filtrada.</param>
    /// <returns>Lista de professores da especialidade.</returns>
    public async Task<List<Professor>> ObterPorEspecialidade(string especialidade)
    {
        try
        {
            var professores = await _context.Professores
                .Where(p => p.Ativo && p.Especialidade != null && p.Especialidade.Contains(especialidade))
                .OrderBy(p => p.Nome)
                .ToListAsync();

            return professores;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao obter professores por especialidade. Especialidade: {especialidade}");
            return new List<Professor>();
        }
    }

    /// <summary>
    /// Obtém professores com mais experiência.
    /// </summary>
    /// <param name="limite">O número máximo de professores a retornar.</param>
    /// <returns>Lista de professores com mais experiência.</returns>
    public async Task<List<ProfessorExperiencia>> ObterMaisExperientes(int limite = 10)
    {
        try
        {
            var professores = await _context.Professores
                .Where(p => p.Ativo)
                .Select(p => new ProfessorExperiencia
                {
                    ProfessorId = p.Id,
                    Nome = p.Nome,
                    Especialidade = p.Especialidade,
                    DataContratacao = p.DataContratacao,
                    TempoExperiencia = CalcularTempoExperiencia(p.DataContratacao),
                    TotalDisciplinas = p.Disciplinas.Count(d => d.Ativa)
                })
                .OrderByDescending(p => p.TempoExperiencia)
                .Take(limite)
                .ToListAsync();

            return professores;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter professores mais experientes");
            return new List<ProfessorExperiencia>();
        }
    }

    /// <summary>
    /// Verifica se um professor pode ser excluído.
    /// </summary>
    /// <param name="professorId">O ID do professor.</param>
    /// <returns>True se pode ser excluído, false caso contrário.</returns>
    public async Task<bool> PodeSerExcluido(int professorId)
    {
        try
        {
            var temDisciplinasAtivas = await _context.Disciplinas
                .AnyAsync(d => d.ProfessorId == professorId && d.Ativa);

            return !temDisciplinasAtivas;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao verificar se professor pode ser excluído. ID: {professorId}");
            return false;
        }
    }
}

/// <summary>
/// Representa estatísticas de um professor.
/// </summary>
public class ProfessorEstatisticas
{
    /// <summary>
    /// Obtém ou define o ID do professor.
    /// </summary>
    public int ProfessorId { get; set; }

    /// <summary>
    /// Obtém ou define o nome do professor.
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a especialidade do professor.
    /// </summary>
    public string? Especialidade { get; set; }

    /// <summary>
    /// Obtém ou define o tempo de experiência em anos.
    /// </summary>
    public int TempoExperiencia { get; set; }

    /// <summary>
    /// Obtém ou define o total de disciplinas.
    /// </summary>
    public int TotalDisciplinas { get; set; }

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
/// Representa a experiência de um professor.
/// </summary>
public class ProfessorExperiencia
{
    /// <summary>
    /// Obtém ou define o ID do professor.
    /// </summary>
    public int ProfessorId { get; set; }

    /// <summary>
    /// Obtém ou define o nome do professor.
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a especialidade do professor.
    /// </summary>
    public string? Especialidade { get; set; }

    /// <summary>
    /// Obtém ou define a data de contratação.
    /// </summary>
    public DateTime DataContratacao { get; set; }

    /// <summary>
    /// Obtém ou define o tempo de experiência em anos.
    /// </summary>
    public int TempoExperiencia { get; set; }

    /// <summary>
    /// Obtém ou define o total de disciplinas.
    /// </summary>
    public int TotalDisciplinas { get; set; }
}
