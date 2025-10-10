using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

/// <summary>
/// Controlador para operações CRUD de alunos com autenticação JWT.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize] // Requer autenticação JWT
public class AlunoController : ControllerBase
{
    private readonly IRepository<Aluno> _alunoRepository;
    private readonly ILogger<AlunoController> _logger;

    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="AlunoController"/>.
    /// </summary>
    /// <param name="alunoRepository">O repositório de alunos.</param>
    /// <param name="logger">O logger.</param>
    public AlunoController(IRepository<Aluno> alunoRepository, ILogger<AlunoController> logger)
    {
        _alunoRepository = alunoRepository;
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
            var alunos = _alunoRepository.Listar();
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
    /// Busca alunos por propriedade e valor.
    /// </summary>
    /// <param name="propriedade">A propriedade a ser pesquisada.</param>
    /// <param name="valor">O valor a ser pesquisado.</param>
    /// <returns>Uma lista de alunos correspondentes.</returns>
    [HttpGet("buscar")]
    public async Task<IActionResult> Buscar([FromQuery] string propriedade, [FromQuery] string valor)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(propriedade) || string.IsNullOrWhiteSpace(valor))
            {
                return BadRequest(new { message = "Propriedade e valor são obrigatórios." });
            }

            var alunos = _alunoRepository.Buscar(propriedade, valor);
            _logger.LogInformation($"Busca realizada por {propriedade}={valor}. Resultados: {alunos.Count}");
            return Ok(alunos);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, $"Propriedade inválida para busca: {propriedade}");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao buscar alunos");
            return StatusCode(500, new { message = "Erro interno do servidor." });
        }
    }

    /// <summary>
    /// Insere um novo aluno.
    /// </summary>
    /// <param name="request">Os dados do aluno a ser inserido.</param>
    /// <returns>O ID do aluno recém-inserido.</returns>
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

            var id = _alunoRepository.Inserir(request.Nome, request.Idade, request.Email, request.DataNascimento);
            _logger.LogInformation($"Aluno inserido com sucesso. ID: {id}, Nome: {request.Nome}");
            
            return CreatedAtAction(nameof(Buscar), new { propriedade = "Id", valor = id.ToString() }, new { id, message = "Aluno inserido com sucesso." });
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

            var rowsAffected = _alunoRepository.Atualizar(id, request.Nome, request.Idade, request.Email, request.DataNascimento);
            
            if (rowsAffected > 0)
            {
                _logger.LogInformation($"Aluno atualizado com sucesso. ID: {id}");
                return Ok(new { message = "Aluno atualizado com sucesso." });
            }
            else
            {
                _logger.LogWarning($"Nenhum aluno encontrado para atualização. ID: {id}");
                return NotFound(new { message = "Aluno não encontrado." });
            }
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
            var rowsAffected = _alunoRepository.Excluir(id);
            
            if (rowsAffected > 0)
            {
                _logger.LogInformation($"Aluno excluído com sucesso. ID: {id}");
                return Ok(new { message = "Aluno excluído com sucesso." });
            }
            else
            {
                _logger.LogWarning($"Nenhum aluno encontrado para exclusão. ID: {id}");
                return NotFound(new { message = "Aluno não encontrado." });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao excluir aluno. ID: {id}");
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
