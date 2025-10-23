using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ADOLabDbContext = ADOLabDbContext;

/// <summary>
/// Controlador para operações CRUD de disciplinas com autenticação JWT e EF Core.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Requer autenticação JWT
public class DisciplinaController : ControllerBase
{
    private readonly ADOLabDbContext _context;
    private readonly ILogger<DisciplinaController> _logger;

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="DisciplinaController"/>.
    /// </summary>
    /// <param name="context">O contexto do banco de dados.</param>
    /// <param name="logger">O logger.</param>
    public DisciplinaController(ADOLabDbContext context, ILogger<DisciplinaController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lista todas as disciplinas.
    /// </summary>
    /// <returns>Uma lista de disciplinas.</returns>
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        try
        {
            var disciplinas = await _context.Disciplinas
                .Include(d => d.Professor)
                .Where(d => d.Ativa)
                .OrderBy(d => d.Nome)
                .ToListAsync();

            _logger.LogInformation($"Listagem de disciplinas solicitada. Total: {disciplinas.Count}");
            return Ok(disciplinas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar disciplinas");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Obtém uma disciplina por ID.
    /// </summary>
    /// <param name="id">O ID da disciplina.</param>
    /// <returns>A disciplina encontrada.</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> ObterPorId(int id)
    {
        try
        {
            var disciplina = await _context.Disciplinas
                .Include(d => d.Professor)
                .Include(d => d.Matriculas)
                    .ThenInclude(m => m.Aluno)
                .FirstOrDefaultAsync(d => d.Id == id && d.Ativa);

            if (disciplina == null)
            {
                _logger.LogWarning($"Disciplina não encontrada. ID: {id}");
                return NotFound(new { message = "Disciplina não encontrada." });
            }

            _logger.LogInformation($"Disciplina encontrada. ID: {id}, Nome: {disciplina.Nome}");
            return Ok(disciplina);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao obter disciplina. ID: {id}");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Busca disciplinas por nome, código ou descrição.
    /// </summary>
    /// <param name="termo">O termo de busca.</param>
    /// <returns>Uma lista de disciplinas correspondentes.</returns>
    [HttpGet("buscar")]
    public async Task<IActionResult> Buscar([FromQuery] string termo)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(termo))
            {
                return BadRequest(new { message = "Termo de busca é obrigatório." });
            }

            var disciplinas = await _context.Disciplinas
                .Include(d => d.Professor)
                .Where(d => d.Ativa && 
                           (d.Nome.Contains(termo) || 
                            d.Codigo.Contains(termo) ||
                            (d.Descricao != null && d.Descricao.Contains(termo))))
                .OrderBy(d => d.Nome)
                .ToListAsync();

            _logger.LogInformation($"Busca realizada por '{termo}'. Resultados: {disciplinas.Count}");
            return Ok(disciplinas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao buscar disciplinas. Termo: {termo}");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Insere uma nova disciplina.
    /// </summary>
    /// <param name="request">Os dados da disciplina a ser inserida.</param>
    /// <returns>A disciplina recém-inserida.</returns>
    [HttpPost]
    public async Task<IActionResult> Inserir([FromBody] CreateDisciplinaRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Nome) || 
                string.IsNullOrWhiteSpace(request.Codigo))
            {
                return BadRequest(new { message = "Nome e código são obrigatórios." });
            }

            // Verificar se o código já existe
            var codigoExiste = await _context.Disciplinas
                .AnyAsync(d => d.Codigo == request.Codigo);

            if (codigoExiste)
            {
                return BadRequest(new { message = "Código da disciplina já cadastrado." });
            }

            // Verificar se o professor existe e está ativo
            var professor = await _context.Professores
                .FirstOrDefaultAsync(p => p.Id == request.ProfessorId && p.Ativo);

            if (professor == null)
            {
                return BadRequest(new { message = "Professor não encontrado ou inativo." });
            }

            var disciplina = new Disciplina
            {
                Nome = request.Nome,
                Codigo = request.Codigo,
                CargaHoraria = request.CargaHoraria,
                Descricao = request.Descricao,
                ProfessorId = request.ProfessorId,
                Ativa = true
            };

            _context.Disciplinas.Add(disciplina);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Disciplina inserida com sucesso. ID: {disciplina.Id}, Nome: {disciplina.Nome}");
            
            return CreatedAtAction(nameof(ObterPorId), new { id = disciplina.Id }, disciplina);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao inserir disciplina");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Atualiza uma disciplina existente.
    /// </summary>
    /// <param name="id">O ID da disciplina a ser atualizada.</param>
    /// <param name="request">Os novos dados da disciplina.</param>
    /// <returns>Resultado da operação.</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] UpdateDisciplinaRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Nome) || 
                string.IsNullOrWhiteSpace(request.Codigo))
            {
                return BadRequest(new { message = "Nome e código são obrigatórios." });
            }

            var disciplina = await _context.Disciplinas
                .FirstOrDefaultAsync(d => d.Id == id && d.Ativa);

            if (disciplina == null)
            {
                _logger.LogWarning($"Disciplina não encontrada para atualização. ID: {id}");
                return NotFound(new { message = "Disciplina não encontrada." });
            }

            // Verificar se o código já existe em outra disciplina
            var codigoExiste = await _context.Disciplinas
                .AnyAsync(d => d.Codigo == request.Codigo && d.Id != id);

            if (codigoExiste)
            {
                return BadRequest(new { message = "Código da disciplina já cadastrado para outra disciplina." });
            }

            // Verificar se o professor existe e está ativo
            var professor = await _context.Professores
                .FirstOrDefaultAsync(p => p.Id == request.ProfessorId && p.Ativo);

            if (professor == null)
            {
                return BadRequest(new { message = "Professor não encontrado ou inativo." });
            }

            disciplina.Nome = request.Nome;
            disciplina.Codigo = request.Codigo;
            disciplina.CargaHoraria = request.CargaHoraria;
            disciplina.Descricao = request.Descricao;
            disciplina.ProfessorId = request.ProfessorId;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Disciplina atualizada com sucesso. ID: {id}");
            return Ok(new { message = "Disciplina atualizada com sucesso." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao atualizar disciplina. ID: {id}");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Exclui uma disciplina (soft delete).
    /// </summary>
    /// <param name="id">O ID da disciplina a ser excluída.</param>
    /// <returns>Resultado da operação.</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Excluir(int id)
    {
        try
        {
            var disciplina = await _context.Disciplinas
                .FirstOrDefaultAsync(d => d.Id == id && d.Ativa);

            if (disciplina == null)
            {
                _logger.LogWarning($"Disciplina não encontrada para exclusão. ID: {id}");
                return NotFound(new { message = "Disciplina não encontrada." });
            }

            // Verificar se a disciplina tem matrículas ativas
            var temMatriculasAtivas = await _context.Matriculas
                .AnyAsync(m => m.DisciplinaId == id && m.Status == "Ativa");

            if (temMatriculasAtivas)
            {
                return BadRequest(new { message = "Não é possível excluir disciplina com matrículas ativas." });
            }

            disciplina.Ativa = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Disciplina excluída com sucesso. ID: {id}");
            return Ok(new { message = "Disciplina excluída com sucesso." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao excluir disciplina. ID: {id}");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Obtém as matrículas de uma disciplina.
    /// </summary>
    /// <param name="id">O ID da disciplina.</param>
    /// <returns>As matrículas da disciplina.</returns>
    [HttpGet("{id}/matriculas")]
    public async Task<IActionResult> ObterMatriculas(int id)
    {
        try
        {
            var disciplina = await _context.Disciplinas
                .FirstOrDefaultAsync(d => d.Id == id && d.Ativa);

            if (disciplina == null)
            {
                return NotFound(new { message = "Disciplina não encontrada." });
            }

            var matriculas = await _context.Matriculas
                .Include(m => m.Aluno)
                .Where(m => m.DisciplinaId == id)
                .OrderBy(m => m.Aluno.Nome)
                .ToListAsync();

            _logger.LogInformation($"Matrículas da disciplina {id} solicitadas. Total: {matriculas.Count}");
            return Ok(matriculas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao obter matrículas da disciplina. ID: {id}");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Obtém disciplinas por professor.
    /// </summary>
    /// <param name="professorId">O ID do professor.</param>
    /// <returns>As disciplinas do professor.</returns>
    [HttpGet("por-professor/{professorId}")]
    public async Task<IActionResult> ObterPorProfessor(int professorId)
    {
        try
        {
            var professor = await _context.Professores
                .FirstOrDefaultAsync(p => p.Id == professorId && p.Ativo);

            if (professor == null)
            {
                return NotFound(new { message = "Professor não encontrado." });
            }

            var disciplinas = await _context.Disciplinas
                .Where(d => d.ProfessorId == professorId && d.Ativa)
                .OrderBy(d => d.Nome)
                .ToListAsync();

            _logger.LogInformation($"Disciplinas do professor {professorId} solicitadas. Total: {disciplinas.Count}");
            return Ok(disciplinas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao obter disciplinas do professor. ID: {professorId}");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }
}

/// <summary>
/// Representa uma solicitação de criação de disciplina.
/// </summary>
public class CreateDisciplinaRequest
{
    /// <summary>
    /// Obtém ou define o nome da disciplina.
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define o código da disciplina.
    /// </summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a carga horária da disciplina.
    /// </summary>
    public int CargaHoraria { get; set; }

    /// <summary>
    /// Obtém ou define a descrição da disciplina.
    /// </summary>
    public string? Descricao { get; set; }

    /// <summary>
    /// Obtém ou define o ID do professor responsável.
    /// </summary>
    public int ProfessorId { get; set; }
}

/// <summary>
/// Representa uma solicitação de atualização de disciplina.
/// </summary>
public class UpdateDisciplinaRequest
{
    /// <summary>
    /// Obtém ou define o nome da disciplina.
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define o código da disciplina.
    /// </summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a carga horária da disciplina.
    /// </summary>
    public int CargaHoraria { get; set; }

    /// <summary>
    /// Obtém ou define a descrição da disciplina.
    /// </summary>
    public string? Descricao { get; set; }

    /// <summary>
    /// Obtém ou define o ID do professor responsável.
    /// </summary>
    public int ProfessorId { get; set; }
}
