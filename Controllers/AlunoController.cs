using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ADOLabDbContext = ADOLabDbContext;

/// <summary>
/// Controlador para operações CRUD de alunos com autenticação JWT e EF Core.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Requer autenticação JWT
public class AlunoController : ControllerBase
{
    private readonly ADOLabDbContext _context;
    private readonly ILogger<AlunoController> _logger;

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="AlunoController"/>.
    /// </summary>
    /// <param name="context">O contexto do banco de dados.</param>
    /// <param name="logger">O logger.</param>
    public AlunoController(ADOLabDbContext context, ILogger<AlunoController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Lista todos os alunos.
    /// </summary>
    /// <returns>Uma lista de alunos.</returns>
    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        try
        {
            var alunos = await _context.Alunos
                .OrderBy(a => a.Nome)
                .ToListAsync();

            _logger.LogInformation($"Listagem de alunos solicitada. Total: {alunos.Count}");
            return Ok(alunos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao listar alunos");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Obtém um aluno por ID.
    /// </summary>
    /// <param name="id">O ID do aluno.</param>
    /// <returns>O aluno encontrado.</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> ObterPorId(int id)
    {
        try
        {
            var aluno = await _context.Alunos
                .Include(a => a.Matriculas)
                    .ThenInclude(m => m.Disciplina)
                        .ThenInclude(d => d.Professor)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (aluno == null)
            {
                _logger.LogWarning($"Aluno não encontrado. ID: {id}");
                return NotFound(new { message = "Aluno não encontrado." });
            }

            _logger.LogInformation($"Aluno encontrado. ID: {id}, Nome: {aluno.Nome}");
            return Ok(aluno);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao obter aluno. ID: {id}");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Busca alunos por nome ou email.
    /// </summary>
    /// <param name="termo">O termo de busca.</param>
    /// <returns>Uma lista de alunos correspondentes.</returns>
    [HttpGet("buscar")]
    public async Task<IActionResult> Buscar([FromQuery] string termo)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(termo))
            {
                return BadRequest(new { message = "Termo de busca é obrigatório." });
            }

            var alunos = await _context.Alunos
                .Where(a => a.Nome.Contains(termo) || a.Email.Contains(termo))
                .OrderBy(a => a.Nome)
                .ToListAsync();

            _logger.LogInformation($"Busca realizada por '{termo}'. Resultados: {alunos.Count}");
            return Ok(alunos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao buscar alunos. Termo: {termo}");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Insere um novo aluno.
    /// </summary>
    /// <param name="request">Os dados do aluno a ser inserido.</param>
    /// <returns>O aluno recém-inserido.</returns>
    [HttpPost]
    public async Task<IActionResult> Inserir([FromBody] CreateAlunoRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Nome) || 
                string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { message = "Nome e email são obrigatórios." });
            }

            if (request.Idade <= 0)
            {
                return BadRequest(new { message = "Idade deve ser maior que zero." });
            }

            // Verificar se o email já existe
            var emailExiste = await _context.Alunos
                .AnyAsync(a => a.Email == request.Email);

            if (emailExiste)
            {
                return BadRequest(new { message = "Email já cadastrado." });
            }

            var aluno = new Aluno
            {
                Nome = request.Nome,
                Idade = request.Idade,
                Email = request.Email,
                DataNascimento = request.DataNascimento
            };

            _context.Alunos.Add(aluno);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Aluno inserido com sucesso. ID: {aluno.Id}, Nome: {aluno.Nome}");
            
            return CreatedAtAction(nameof(ObterPorId), new { id = aluno.Id }, aluno);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao inserir aluno");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Atualiza um aluno existente.
    /// </summary>
    /// <param name="id">O ID do aluno a ser atualizado.</param>
    /// <param name="request">Os novos dados do aluno.</param>
    /// <returns>Resultado da operação.</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> Atualizar(int id, [FromBody] UpdateAlunoRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Nome) || 
                string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { message = "Nome e email são obrigatórios." });
            }

            if (request.Idade <= 0)
            {
                return BadRequest(new { message = "Idade deve ser maior que zero." });
            }

            var aluno = await _context.Alunos
                .FirstOrDefaultAsync(a => a.Id == id);

            if (aluno == null)
            {
                _logger.LogWarning($"Aluno não encontrado para atualização. ID: {id}");
                return NotFound(new { message = "Aluno não encontrado." });
            }

            // Verificar se o email já existe em outro aluno
            var emailExiste = await _context.Alunos
                .AnyAsync(a => a.Email == request.Email && a.Id != id);

            if (emailExiste)
            {
                return BadRequest(new { message = "Email já cadastrado para outro aluno." });
            }

            aluno.Nome = request.Nome;
            aluno.Idade = request.Idade;
            aluno.Email = request.Email;
            aluno.DataNascimento = request.DataNascimento;

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Aluno atualizado com sucesso. ID: {id}");
            return Ok(new { message = "Aluno atualizado com sucesso." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao atualizar aluno. ID: {id}");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Exclui um aluno.
    /// </summary>
    /// <param name="id">O ID do aluno a ser excluído.</param>
    /// <returns>Resultado da operação.</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Excluir(int id)
    {
        try
        {
            var aluno = await _context.Alunos
                .Include(a => a.Matriculas)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (aluno == null)
            {
                _logger.LogWarning($"Aluno não encontrado para exclusão. ID: {id}");
                return NotFound(new { message = "Aluno não encontrado." });
            }

            // Verificar se o aluno tem matrículas ativas
            var temMatriculasAtivas = aluno.Matriculas.Any(m => m.Status == "Ativa");

            if (temMatriculasAtivas)
            {
                return BadRequest(new { message = "Não é possível excluir aluno com matrículas ativas." });
            }

            _context.Alunos.Remove(aluno);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Aluno excluído com sucesso. ID: {id}");
            return Ok(new { message = "Aluno excluído com sucesso." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao excluir aluno. ID: {id}");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Obtém as matrículas de um aluno.
    /// </summary>
    /// <param name="id">O ID do aluno.</param>
    /// <returns>As matrículas do aluno.</returns>
    [HttpGet("{id}/matriculas")]
    public async Task<IActionResult> ObterMatriculas(int id)
    {
        try
        {
            var aluno = await _context.Alunos
                .FirstOrDefaultAsync(a => a.Id == id);

            if (aluno == null)
            {
                return NotFound(new { message = "Aluno não encontrado." });
            }

            var matriculas = await _context.Matriculas
                .Include(m => m.Disciplina)
                    .ThenInclude(d => d.Professor)
                .Where(m => m.AlunoId == id)
                .OrderBy(m => m.DataMatricula)
                .ToListAsync();

            _logger.LogInformation($"Matrículas do aluno {id} solicitadas. Total: {matriculas.Count}");
            return Ok(matriculas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao obter matrículas do aluno. ID: {id}");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Obtém informações do usuário autenticado.
    /// </summary>
    /// <returns>Informações do usuário.</returns>
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            var usernameClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Name);
            var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role);

            if (userIdClaim == null || usernameClaim == null)
            {
                return Unauthorized(new { message = "Token inválido." });
            }

            return Ok(new
            {
                userId = userIdClaim.Value,
                username = usernameClaim.Value,
                role = roleClaim?.Value ?? "User",
                message = "Usuário autenticado com sucesso."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao obter informações do usuário atual");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }
}

/// <summary>
/// Representa uma solicitação de criação de aluno.
/// </summary>
public class CreateAlunoRequest
{
    /// <summary>
    /// Obtém ou define o nome do aluno.
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a idade do aluno.
    /// </summary>
    public int Idade { get; set; }

    /// <summary>
    /// Obtém ou define o email do aluno.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a data de nascimento do aluno.
    /// </summary>
    public DateTime DataNascimento { get; set; }
}

/// <summary>
/// Representa uma solicitação de atualização de aluno.
/// </summary>
public class UpdateAlunoRequest
{
    /// <summary>
    /// Obtém ou define o nome do aluno.
    /// </summary>
    public string Nome { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a idade do aluno.
    /// </summary>
    public int Idade { get; set; }

    /// <summary>
    /// Obtém ou define o email do aluno.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Obtém ou define a data de nascimento do aluno.
    /// </summary>
    public DateTime DataNascimento { get; set; }
}