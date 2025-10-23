using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ADOLabDbContext = ADOLabDbContext;

/// <summary>
/// Controlador para operações CRUD de professores com autenticação JWT e EF Core.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Requer autenticação JWT
public class ProfessorController : ControllerBase
{
    private readonly ADOLabDbContext _context;
    private readonly ILogger<ProfessorController> _logger;

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="ProfessorController"/>.
    /// </summary>
    /// <param name="context">O contexto do banco de dados.</param>
    /// <param name="logger">O logger.</param>
    public ProfessorController(ADOLabDbContext context, ILogger<ProfessorController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lista todos os professores.
    /// </summary>
    /// <returns>Uma lista de professores.</returns>
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        try
        {
            var professores = await _context.Professores
                .Where(p => p.Ativo)
                .OrderBy(p => p.Nome)
                .ToListAsync();

            _logger.LogInformation($"Listagem de professores solicitada. Total: {professores.Count}");
            return Ok(professores);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar professores");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Obtém um professor por ID.
    /// </summary>
    /// <param name="id">O ID do professor.</param>
    /// <returns>O professor encontrado.</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> ObterPorId(int id)
    {
        try
        {
            var professor = await _context.Professores
                .Include(p => p.Disciplinas)
                .FirstOrDefaultAsync(p => p.Id == id && p.Ativo);

            if (professor == null)
            {
                _logger.LogWarning($"Professor não encontrado. ID: {id}");
                return NotFound(new { message = "Professor não encontrado." });
            }

            _logger.LogInformation($"Professor encontrado. ID: {id}, Nome: {professor.Nome}");
            return Ok(professor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao obter professor. ID: {id}");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Busca professores por nome ou especialidade.
    /// </summary>
    /// <param name="termo">O termo de busca.</param>
    /// <returns>Uma lista de professores correspondentes.</returns>
    [HttpGet("buscar")]
    public async Task<IActionResult> Buscar([FromQuery] string termo)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(termo))
            {
                return BadRequest(new { message = "Termo de busca é obrigatório." });
            }

            var professores = await _context.Professores
                .Where(p => p.Ativo && 
                           (p.Nome.Contains(termo) || 
                            (p.Especialidade != null && p.Especialidade.Contains(termo))))
                .OrderBy(p => p.Nome)
                .ToListAsync();

            _logger.LogInformation($"Busca realizada por '{termo}'. Resultados: {professores.Count}");
            return Ok(professores);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao buscar professores. Termo: {termo}");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Insere um novo professor.
    /// </summary>
    /// <param name="request">Os dados do professor a ser inserido.</param>
    /// <returns>O professor recém-inserido.</returns>
    [HttpPost]
    public async Task<IActionResult> Inserir([FromBody] CreateProfessorRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Nome) || 
                string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { message = "Nome e email são obrigatórios." });
            }

            // Verificar se o email já existe
            var emailExiste = await _context.Professores
                .AnyAsync(p => p.Email == request.Email);

            if (emailExiste)
            {
                return BadRequest(new { message = "Email já cadastrado." });
            }

            var professor = new Professor
            {
                Nome = request.Nome,
                Email = request.Email,
                Especialidade = request.Especialidade,
                DataContratacao = request.DataContratacao,
                Ativo = true
            };

            _context.Professores.Add(professor);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Professor inserido com sucesso. ID: {professor.Id}, Nome: {professor.Nome}");
            
            return CreatedAtAction(nameof(ObterPorId), new { id = professor.Id }, professor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao inserir professor");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Atualiza um professor existente.
    /// </summary>
    /// <param name="id">O ID do professor a ser atualizado.</param>
    /// <param name="request">Os novos dados do professor.</param>
    /// <returns>Resultado da operação.</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] UpdateProfessorRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Nome) || 
                string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { message = "Nome e email são obrigatórios." });
            }

            var professor = await _context.Professores
                .FirstOrDefaultAsync(p => p.Id == id && p.Ativo);

            if (professor == null)
            {
                _logger.LogWarning($"Professor não encontrado para atualização. ID: {id}");
                return NotFound(new { message = "Professor não encontrado." });
            }

            // Verificar se o email já existe em outro professor
            var emailExiste = await _context.Professores
                .AnyAsync(p => p.Email == request.Email && p.Id != id);

            if (emailExiste)
            {
                return BadRequest(new { message = "Email já cadastrado para outro professor." });
            }

            professor.Nome = request.Nome;
            professor.Email = request.Email;
            professor.Especialidade = request.Especialidade;
            professor.DataContratacao = request.DataContratacao;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Professor atualizado com sucesso. ID: {id}");
            return Ok(new { message = "Professor atualizado com sucesso." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao atualizar professor. ID: {id}");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Exclui um professor (soft delete).
    /// </summary>
    /// <param name="id">O ID do professor a ser excluído.</param>
    /// <returns>Resultado da operação.</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Excluir(int id)
    {
        try
        {
            var professor = await _context.Professores
                .FirstOrDefaultAsync(p => p.Id == id && p.Ativo);

            if (professor == null)
            {
                _logger.LogWarning($"Professor não encontrado para exclusão. ID: {id}");
                return NotFound(new { message = "Professor não encontrado." });
            }

            // Verificar se o professor tem disciplinas ativas
            var temDisciplinasAtivas = await _context.Disciplinas
                .AnyAsync(d => d.ProfessorId == id && d.Ativa);

            if (temDisciplinasAtivas)
            {
                return BadRequest(new { message = "Não é possível excluir professor com disciplinas ativas." });
            }

            professor.Ativo = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Professor excluído com sucesso. ID: {id}");
            return Ok(new { message = "Professor excluído com sucesso." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao excluir professor. ID: {id}");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Obtém as disciplinas de um professor.
    /// </summary>
    /// <param name="id">O ID do professor.</param>
    /// <returns>As disciplinas do professor.</returns>
    [HttpGet("{id}/disciplinas")]
    public async Task<IActionResult> ObterDisciplinas(int id)
    {
        try
        {
            var professor = await _context.Professores
                .FirstOrDefaultAsync(p => p.Id == id && p.Ativo);

            if (professor == null)
            {
                return NotFound(new { message = "Professor não encontrado." });
            }

            var disciplinas = await _context.Disciplinas
                .Where(d => d.ProfessorId == id && d.Ativa)
                .OrderBy(d => d.Nome)
                .ToListAsync();

            _logger.LogInformation($"Disciplinas do professor {id} solicitadas. Total: {disciplinas.Count}");
            return Ok(disciplinas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao obter disciplinas do professor. ID: {id}");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }
}

/// <summary>
/// Representa uma solicitação de criação de professor.
/// </summary>
public class CreateProfessorRequest
{
    /// <summary>
    /// Obtém ou define o nome do professor.
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define o email do professor.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a especialidade do professor.
    /// </summary>
    public string? Especialidade { get; set; }

    /// <summary>
    /// Obtém ou define a data de contratação do professor.
    /// </summary>
    public DateTime DataContratacao { get; set; }
}

/// <summary>
/// Representa uma solicitação de atualização de professor.
/// </summary>
public class UpdateProfessorRequest
{
    /// <summary>
    /// Obtém ou define o nome do professor.
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define o email do professor.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a especialidade do professor.
    /// </summary>
    public string? Especialidade { get; set; }

    /// <summary>
    /// Obtém ou define a data de contratação do professor.
    /// </summary>
    public DateTime DataContratacao { get; set; }
}
