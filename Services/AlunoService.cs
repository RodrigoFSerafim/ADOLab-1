using Microsoft.EntityFrameworkCore;
using ADOLabDbContext = ADOLabDbContext;

/// <summary>
/// Serviço para lógica de negócio relacionada a alunos.
/// </summary>
public class AlunoService
{
    private readonly ADOLabDbContext _context;
    private readonly ILogger<AlunoService> _logger;

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="AlunoService"/>.
    /// </summary>
    /// <param name="context">O contexto do banco de dados.</param>
    /// <param name="logger">O logger.</param>
    public AlunoService(ADOLabDbContext context, ILogger<AlunoService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Calcula a idade baseada na data de nascimento.
    /// </summary>
    /// <param name="dataNascimento">A data de nascimento.</param>
    /// <returns>A idade calculada.</returns>
    public int CalcularIdade(DateTime dataNascimento)
    {
        var hoje = DateTime.Today;
        var idade = hoje.Year - dataNascimento.Year;
        
        if (dataNascimento.Date > hoje.AddYears(-idade))
            idade--;

        return idade;
    }

    /// <summary>
    /// Valida se um email é válido.
    /// </summary>
    /// <param name="email">O email a ser validado.</param>
    /// <returns>True se o email é válido, false caso contrário.</returns>
    public bool ValidarEmail(string email)
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

    /// <summary>
    /// Obtém estatísticas de um aluno.
    /// </summary>
    /// <param name="alunoId">O ID do aluno.</param>
    /// <returns>Estatísticas do aluno.</returns>
    public async Task<AlunoEstatisticas?> ObterEstatisticas(int alunoId)
    {
        try
        {
            var aluno = await _context.Alunos
                .Include(a => a.Matriculas)
                    .ThenInclude(m => m.Disciplina)
                .FirstOrDefaultAsync(a => a.Id == alunoId);

            if (aluno == null)
                return null;

            var matriculas = aluno.Matriculas;
            var totalDisciplinas = matriculas.Count;
            var disciplinasConcluidas = matriculas.Count(m => m.Status == "Concluída");
            var disciplinasAtivas = matriculas.Count(m => m.Status == "Ativa");
            var disciplinasReprovadas = matriculas.Count(m => m.Status == "Reprovada");

            var mediaGeral = matriculas
                .Where(m => m.NotaFinal.HasValue)
                .Average(m => m.NotaFinal!.Value);

            var frequenciaMedia = matriculas
                .Where(m => m.Frequencia.HasValue)
                .Average(m => m.Frequencia!.Value);

            return new AlunoEstatisticas
            {
                AlunoId = alunoId,
                Nome = aluno.Nome,
                TotalDisciplinas = totalDisciplinas,
                DisciplinasConcluidas = disciplinasConcluidas,
                DisciplinasAtivas = disciplinasAtivas,
                DisciplinasReprovadas = disciplinasReprovadas,
                MediaGeral = mediaGeral,
                FrequenciaMedia = frequenciaMedia
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao obter estatísticas do aluno. ID: {alunoId}");
            return null;
        }
    }

    /// <summary>
    /// Obtém alunos com melhor desempenho.
    /// </summary>
    /// <param name="limite">O número máximo de alunos a retornar.</param>
    /// <returns>Lista de alunos com melhor desempenho.</returns>
    public async Task<List<AlunoDesempenho>> ObterMelhoresAlunos(int limite = 10)
    {
        try
        {
            var alunos = await _context.Alunos
                .Include(a => a.Matriculas)
                .Where(a => a.Matriculas.Any(m => m.NotaFinal.HasValue))
                .Select(a => new AlunoDesempenho
                {
                    AlunoId = a.Id,
                    Nome = a.Nome,
                    Email = a.Email,
                    MediaGeral = a.Matriculas
                        .Where(m => m.NotaFinal.HasValue)
                        .Average(m => m.NotaFinal!.Value),
                    TotalDisciplinas = a.Matriculas.Count,
                    DisciplinasConcluidas = a.Matriculas.Count(m => m.Status == "Concluída")
                })
                .OrderByDescending(a => a.MediaGeral)
                .Take(limite)
                .ToListAsync();

            return alunos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter melhores alunos");
            return new List<AlunoDesempenho>();
        }
    }
}

/// <summary>
/// Representa estatísticas de um aluno.
/// </summary>
public class AlunoEstatisticas
{
    /// <summary>
    /// Obtém ou define o ID do aluno.
    /// </summary>
    public int AlunoId { get; set; }

    /// <summary>
    /// Obtém ou define o nome do aluno.
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define o total de disciplinas.
    /// </summary>
    public int TotalDisciplinas { get; set; }

    /// <summary>
    /// Obtém ou define o número de disciplinas concluídas.
    /// </summary>
    public int DisciplinasConcluidas { get; set; }

    /// <summary>
    /// Obtém ou define o número de disciplinas ativas.
    /// </summary>
    public int DisciplinasAtivas { get; set; }

    /// <summary>
    /// Obtém ou define o número de disciplinas reprovadas.
    /// </summary>
    public int DisciplinasReprovadas { get; set; }

    /// <summary>
    /// Obtém ou define a média geral.
    /// </summary>
    public double MediaGeral { get; set; }

    /// <summary>
    /// Obtém ou define a frequência média.
    /// </summary>
    public double FrequenciaMedia { get; set; }
}

/// <summary>
/// Representa o desempenho de um aluno.
/// </summary>
public class AlunoDesempenho
{
    /// <summary>
    /// Obtém ou define o ID do aluno.
    /// </summary>
    public int AlunoId { get; set; }

    /// <summary>
    /// Obtém ou define o nome do aluno.
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define o email do aluno.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a média geral.
    /// </summary>
    public double MediaGeral { get; set; }

    /// <summary>
    /// Obtém ou define o total de disciplinas.
    /// </summary>
    public int TotalDisciplinas { get; set; }

    /// <summary>
    /// Obtém ou define o número de disciplinas concluídas.
    /// </summary>
    public int DisciplinasConcluidas { get; set; }
}
