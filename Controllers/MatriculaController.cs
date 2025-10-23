using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ADOLabDbContext = ADOLabDbContext;

/// <summary>
/// Controlador para operações CRUD de matrículas com autenticação JWT e EF Core.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Requer autenticação JWT
public class MatriculaController : ControllerBase
{
    private readonly ADOLabDbContext _context;
    private readonly ILogger<MatriculaController> _logger;

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="MatriculaController"/>.
    /// </summary>
    /// <param name="context">O contexto do banco de dados.</param>
    /// <param name="logger">O logger.</param>
    public MatriculaController(ADOLabDbContext context, ILogger<MatriculaController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lista todas as matrículas.
    /// </summary>
    /// <returns>Uma lista de matrículas.</returns>
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        try
        {
            var matriculas = await _context.Matriculas
                .Include(m => m.Aluno)
                .Include(m => m.Disciplina)
                    .ThenInclude(d => d.Professor)
                .OrderBy(m => m.DataMatricula)
                .ToListAsync();

            _logger.LogInformation($"Listagem de matrículas solicitada. Total: {matriculas.Count}");
            return Ok(matriculas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar matrículas");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Obtém uma matrícula por ID.
    /// </summary>
    /// <param name="id">O ID da matrícula.</param>
    /// <returns>A matrícula encontrada.</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> ObterPorId(int id)
    {
        try
        {
            var matricula = await _context.Matriculas
                .Include(m => m.Aluno)
                .Include(m => m.Disciplina)
                    .ThenInclude(d => d.Professor)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (matricula == null)
            {
                _logger.LogWarning($"Matrícula não encontrada. ID: {id}");
                return NotFound(new { message = "Matrícula não encontrada." });
            }

            _logger.LogInformation($"Matrícula encontrada. ID: {id}, Aluno: {matricula.Aluno.Nome}, Disciplina: {matricula.Disciplina.Nome}");
            return Ok(matricula);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao obter matrícula. ID: {id}");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Busca matrículas por aluno ou disciplina.
    /// </summary>
    /// <param name="alunoId">ID do aluno (opcional).</param>
    /// <param name="disciplinaId">ID da disciplina (opcional).</param>
    /// <param name="status">Status da matrícula (opcional).</param>
    /// <returns>Uma lista de matrículas correspondentes.</returns>
    [HttpGet("buscar")]
    public async Task<IActionResult> Buscar([FromQuery] int? alunoId, [FromQuery] int? disciplinaId, [FromQuery] string? status)
    {
        try
        {
            var query = _context.Matriculas
                .Include(m => m.Aluno)
                .Include(m => m.Disciplina)
                    .ThenInclude(d => d.Professor)
                .AsQueryable();

            if (alunoId.HasValue)
            {
                query = query.Where(m => m.AlunoId == alunoId.Value);
            }

            if (disciplinaId.HasValue)
            {
                query = query.Where(m => m.DisciplinaId == disciplinaId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(m => m.Status == status);
            }

            var matriculas = await query
                .OrderBy(m => m.DataMatricula)
                .ToListAsync();

            _logger.LogInformation($"Busca de matrículas realizada. Filtros: AlunoId={alunoId}, DisciplinaId={disciplinaId}, Status={status}. Resultados: {matriculas.Count}");
            return Ok(matriculas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar matrículas");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Insere uma nova matrícula.
    /// </summary>
    /// <param name="request">Os dados da matrícula a ser inserida.</param>
    /// <returns>A matrícula recém-inserida.</returns>
    [HttpPost]
    public async Task<IActionResult> Inserir([FromBody] CreateMatriculaRequest request)
    {
        try
        {
            // Verificar se o aluno existe
            var aluno = await _context.Alunos
                .FirstOrDefaultAsync(a => a.Id == request.AlunoId);

            if (aluno == null)
            {
                return BadRequest(new { message = "Aluno não encontrado." });
            }

            // Verificar se a disciplina existe e está ativa
            var disciplina = await _context.Disciplinas
                .FirstOrDefaultAsync(d => d.Id == request.DisciplinaId && d.Ativa);

            if (disciplina == null)
            {
                return BadRequest(new { message = "Disciplina não encontrada ou inativa." });
            }

            // Verificar se já existe matrícula para este aluno nesta disciplina
            var matriculaExistente = await _context.Matriculas
                .AnyAsync(m => m.AlunoId == request.AlunoId && m.DisciplinaId == request.DisciplinaId);

            if (matriculaExistente)
            {
                return BadRequest(new { message = "Aluno já está matriculado nesta disciplina." });
            }

            var matricula = new Matricula
            {
                AlunoId = request.AlunoId,
                DisciplinaId = request.DisciplinaId,
                DataMatricula = request.DataMatricula,
                Status = request.Status ?? "Ativa"
            };

            _context.Matriculas.Add(matricula);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Matrícula inserida com sucesso. ID: {matricula.Id}, Aluno: {aluno.Nome}, Disciplina: {disciplina.Nome}");
            
            return CreatedAtAction(nameof(ObterPorId), new { id = matricula.Id }, matricula);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao inserir matrícula");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Atualiza uma matrícula existente.
    /// </summary>
    /// <param name="id">O ID da matrícula a ser atualizada.</param>
    /// <param name="request">Os novos dados da matrícula.</param>
    /// <returns>Resultado da operação.</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] UpdateMatriculaRequest request)
    {
        try
        {
            var matricula = await _context.Matriculas
                .FirstOrDefaultAsync(m => m.Id == id);

            if (matricula == null)
            {
                _logger.LogWarning($"Matrícula não encontrada para atualização. ID: {id}");
                return NotFound(new { message = "Matrícula não encontrada." });
            }

            // Verificar se o aluno existe (se foi alterado)
            if (request.AlunoId != matricula.AlunoId)
            {
                var aluno = await _context.Alunos
                    .FirstOrDefaultAsync(a => a.Id == request.AlunoId);

                if (aluno == null)
                {
                    return BadRequest(new { message = "Aluno não encontrado." });
                }
            }

            // Verificar se a disciplina existe e está ativa (se foi alterada)
            if (request.DisciplinaId != matricula.DisciplinaId)
            {
                var disciplina = await _context.Disciplinas
                    .FirstOrDefaultAsync(d => d.Id == request.DisciplinaId && d.Ativa);

                if (disciplina == null)
                {
                    return BadRequest(new { message = "Disciplina não encontrada ou inativa." });
                }

                // Verificar se já existe matrícula para este aluno nesta disciplina
                var matriculaExistente = await _context.Matriculas
                    .AnyAsync(m => m.AlunoId == request.AlunoId && m.DisciplinaId == request.DisciplinaId && m.Id != id);

                if (matriculaExistente)
                {
                    return BadRequest(new { message = "Aluno já está matriculado nesta disciplina." });
                }
            }

            matricula.AlunoId = request.AlunoId;
            matricula.DisciplinaId = request.DisciplinaId;
            matricula.DataMatricula = request.DataMatricula;
            matricula.Status = request.Status;
            matricula.NotaFinal = request.NotaFinal;
            matricula.Frequencia = request.Frequencia;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Matrícula atualizada com sucesso. ID: {id}");
            return Ok(new { message = "Matrícula atualizada com sucesso." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao atualizar matrícula. ID: {id}");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Exclui uma matrícula.
    /// </summary>
    /// <param name="id">O ID da matrícula a ser excluída.</param>
    /// <returns>Resultado da operação.</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Excluir(int id)
    {
        try
        {
            var matricula = await _context.Matriculas
                .FirstOrDefaultAsync(m => m.Id == id);

            if (matricula == null)
            {
                _logger.LogWarning($"Matrícula não encontrada para exclusão. ID: {id}");
                return NotFound(new { message = "Matrícula não encontrada." });
            }

            _context.Matriculas.Remove(matricula);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Matrícula excluída com sucesso. ID: {id}");
            return Ok(new { message = "Matrícula excluída com sucesso." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao excluir matrícula. ID: {id}");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Obtém as matrículas de um aluno.
    /// </summary>
    /// <param name="alunoId">O ID do aluno.</param>
    /// <returns>As matrículas do aluno.</returns>
    [HttpGet("aluno/{alunoId}")]
    public async Task<IActionResult> ObterPorAluno(int alunoId)
    {
        try
        {
            var aluno = await _context.Alunos
                .FirstOrDefaultAsync(a => a.Id == alunoId);

            if (aluno == null)
            {
                return NotFound(new { message = "Aluno não encontrado." });
            }

            var matriculas = await _context.Matriculas
                .Include(m => m.Disciplina)
                    .ThenInclude(d => d.Professor)
                .Where(m => m.AlunoId == alunoId)
                .OrderBy(m => m.DataMatricula)
                .ToListAsync();

            _logger.LogInformation($"Matrículas do aluno {alunoId} solicitadas. Total: {matriculas.Count}");
            return Ok(matriculas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao obter matrículas do aluno. ID: {alunoId}");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Obtém as matrículas de uma disciplina.
    /// </summary>
    /// <param name="disciplinaId">O ID da disciplina.</param>
    /// <returns>As matrículas da disciplina.</returns>
    [HttpGet("disciplina/{disciplinaId}")]
    public async Task<IActionResult> ObterPorDisciplina(int disciplinaId)
    {
        try
        {
            var disciplina = await _context.Disciplinas
                .FirstOrDefaultAsync(d => d.Id == disciplinaId && d.Ativa);

            if (disciplina == null)
            {
                return NotFound(new { message = "Disciplina não encontrada." });
            }

            var matriculas = await _context.Matriculas
                .Include(m => m.Aluno)
                .Where(m => m.DisciplinaId == disciplinaId)
                .OrderBy(m => m.Aluno.Nome)
                .ToListAsync();

            _logger.LogInformation($"Matrículas da disciplina {disciplinaId} solicitadas. Total: {matriculas.Count}");
            return Ok(matriculas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao obter matrículas da disciplina. ID: {disciplinaId}");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Atualiza a nota final de uma matrícula.
    /// </summary>
    /// <param name="id">O ID da matrícula.</param>
    /// <param name="request">Os dados da nota.</param>
    /// <returns>Resultado da operação.</returns>
    [HttpPut("{id}/nota")]
    public async Task<IActionResult> AtualizarNota(int id, [FromBody] UpdateNotaRequest request)
    {
        try
        {
            var matricula = await _context.Matriculas
                .FirstOrDefaultAsync(m => m.Id == id);

            if (matricula == null)
            {
                return NotFound(new { message = "Matrícula não encontrada." });
            }

            if (request.NotaFinal < 0 || request.NotaFinal > 10)
            {
                return BadRequest(new { message = "Nota deve estar entre 0 e 10." });
            }

            matricula.NotaFinal = request.NotaFinal;
            matricula.Frequencia = request.Frequencia;

            // Atualizar status baseado na nota
            if (request.NotaFinal >= 6 && (request.Frequencia ?? 0) >= 75)
            {
                matricula.Status = "Concluída";
            }
            else if (request.NotaFinal < 6 || (request.Frequencia ?? 0) < 75)
            {
                matricula.Status = "Reprovada";
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Nota atualizada para matrícula {id}. Nota: {request.NotaFinal}, Frequência: {request.Frequencia}%");
            return Ok(new { message = "Nota atualizada com sucesso." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao atualizar nota da matrícula. ID: {id}");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }
}

/// <summary>
/// Representa uma solicitação de criação de matrícula.
/// </summary>
public class CreateMatriculaRequest
{
    /// <summary>
    /// Obtém ou define o ID do aluno.
    /// </summary>
    public int AlunoId { get; set; }

    /// <summary>
    /// Obtém ou define o ID da disciplina.
    /// </summary>
    public int DisciplinaId { get; set; }

    /// <summary>
    /// Obtém ou define a data da matrícula.
    /// </summary>
    public DateTime DataMatricula { get; set; }

    /// <summary>
    /// Obtém ou define o status da matrícula.
    /// </summary>
    public string? Status { get; set; }
}

/// <summary>
/// Representa uma solicitação de atualização de matrícula.
/// </summary>
public class UpdateMatriculaRequest
{
    /// <summary>
    /// Obtém ou define o ID do aluno.
    /// </summary>
    public int AlunoId { get; set; }

    /// <summary>
    /// Obtém ou define o ID da disciplina.
    /// </summary>
    public int DisciplinaId { get; set; }

    /// <summary>
    /// Obtém ou define a data da matrícula.
    /// </summary>
    public DateTime DataMatricula { get; set; }

    /// <summary>
    /// Obtém ou define o status da matrícula.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a nota final.
    /// </summary>
    public decimal? NotaFinal { get; set; }

    /// <summary>
    /// Obtém ou define a frequência.
    /// </summary>
    public decimal? Frequencia { get; set; }
}

/// <summary>
/// Representa uma solicitação de atualização de nota.
/// </summary>
public class UpdateNotaRequest
{
    /// <summary>
    /// Obtém ou define a nota final.
    /// </summary>
    public decimal NotaFinal { get; set; }

    /// <summary>
    /// Obtém ou define a frequência.
    /// </summary>
    public decimal? Frequencia { get; set; }
}
