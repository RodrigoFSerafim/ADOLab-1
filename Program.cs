using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Builder;

#region Config
// Carrega a connection string do appsettings.json
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

string connString = config.GetConnectionString("SqlServerConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:SqlServerConnection não encontrada.");
#endregion

var builder = WebApplication.CreateBuilder(args);

// Configuração de serviços
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configuração do JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey não encontrada.");
var issuer = jwtSettings["Issuer"] ?? "ADOLab";
var audience = jwtSettings["Audience"] ?? "ADOLabUsers";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Registro de serviços
builder.Services.AddSingleton<IUserRepository>(provider => new UserRepository(connString));
builder.Services.AddSingleton<JwtService>();
builder.Services.AddSingleton<IRepository<Aluno>>(provider => new AlunoRepository(connString));

var app = builder.Build();

// Configuração do pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Inicialização dos esquemas de banco de dados
var logger = new FileLogger("log.txt");
try
{
    // Aplicar migrations do Entity Framework Core
    await MigrationsHelper.ApplyMigrationsAsync(connString);
    
    var alunoRepo = new AlunoRepository(connString);
    var userRepo = new UserRepository(connString);
    
    await logger.LogAsync("Iniciando aplicação e garantindo os esquemas.");
    alunoRepo.GarantirEsquema(); // DDL: cria a tabela Alunos se não existir
    userRepo.GarantirEsquema(); // DDL: cria a tabela Users se não existir
    
    // Criar usuário administrador padrão se não existir
    if (!userRepo.UsernameExiste("admin"))
    {
        var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
        var adminUser = new User("admin", "admin@adolab.com", adminPasswordHash, "Administrador", "Admin");
        userRepo.Inserir(adminUser);
        await logger.LogAsync("Usuário administrador padrão criado (username: admin, password: admin123)");
    }
    
    await logger.LogAsync("Esquemas de banco de dados verificados e usuário admin criado.");
}
catch (Exception ex)
{
    await logger.LogErrorAsync($"Erro ao inicializar esquemas: {ex.Message}");
}

// Menu interativo para operações CRUD
var menuTask = Task.Run(async () =>
{
    var alunoRepo = new AlunoRepository(connString);
    
    while (true)
    {
        Console.WriteLine("\n=== CRUD ADO.NET – Alunos ===");
        Console.WriteLine("1) Inserir");
        Console.WriteLine("2) Listar");
        Console.WriteLine("3) Editar");
        Console.WriteLine("4) Deletar");
        Console.WriteLine("5) Buscar");
        Console.WriteLine("6) Usuários");
        Console.WriteLine("0) Sair");
        Console.Write("Escolha: ");
        var opc = Console.ReadLine();

        if (opc == "0") break;

        try
        {
            switch (opc)
            {
                case "1":
                    Console.Write("Nome: "); var nome = Console.ReadLine() ?? "";
                    Console.Write("Idade: "); var idadeStr = Console.ReadLine();
                    Console.Write("Email: "); var email = Console.ReadLine() ?? "";
                    Console.Write("Data de Nascimento (yyyy-MM-dd): "); var dataNascimentoStr = Console.ReadLine();

                    if (int.TryParse(idadeStr, out int idade) && DateTime.TryParse(dataNascimentoStr, out DateTime dataNascimento))
                    {
                        int id = alunoRepo.Inserir(nome, idade, email, dataNascimento);
                        Console.WriteLine($"✅ Inserido Id={id}");
                        await logger.LogAsync($"Inserido aluno com Id={id}, Nome={nome}, Idade={idade}, Email={email}, DataNascimento={dataNascimento:yyyy-MM-dd}.");
                    }
                    else
                    {
                        Console.WriteLine("Dados inválidos.");
                        await logger.LogWarningAsync("Falha ao inserir aluno devido a dados inválidos.");
                    }
                    break;

                case "2":
                    var alunos = alunoRepo.Listar();
                    Console.WriteLine("== Lista de Alunos ==");
                    foreach (var a in alunos)
                        Console.WriteLine($"#{a.Id} {a.Nome} ({a.Idade}) - {a.Email} - {a.DataNascimento:yyyy-MM-dd}");
                    Console.WriteLine(alunos.Count == 0 ? "(vazio)" : "");
                    await logger.LogAsync("Listou todos os alunos.");
                    break;

                case "3":
                    Console.Write("Id: "); var idEditStr = Console.ReadLine();
                    Console.Write("Novo Nome: "); var novoNome = Console.ReadLine() ?? "";
                    Console.Write("Nova Idade: "); var novaIdadeStr = Console.ReadLine();
                    Console.Write("Novo Email: "); var novoEmail = Console.ReadLine() ?? "";
                    Console.Write("Nova Data de Nascimento (yyyy-MM-dd): "); var novaDataNascimentoStr = Console.ReadLine();

                    if (int.TryParse(idEditStr, out int idEdit) && int.TryParse(novaIdadeStr, out int novaIdade) && DateTime.TryParse(novaDataNascimentoStr, out DateTime novaDataNascimento))
                    {
                        int rows = alunoRepo.Atualizar(idEdit, novoNome, novaIdade, novoEmail, novaDataNascimento);
                        Console.WriteLine(rows > 0 ? "✅ Atualizado." : "⚠️ Nenhum registro afetado.");
                        await logger.LogAsync(rows > 0
                            ? $"Atualizado aluno Id={idEdit} com Nome={novoNome}, Idade={novaIdade}, Email={novoEmail}, DataNascimento={novaDataNascimento:yyyy-MM-dd}."
                            : $"Nenhum registro atualizado para Id={idEdit}.");
                    }
                    else
                    {
                        Console.WriteLine("Dados inválidos.");
                        await logger.LogWarningAsync("Falha ao atualizar aluno devido a dados inválidos.");
                    }
                    break;

                case "4":
                    Console.Write("Id: "); var idDelStr = Console.ReadLine();
                    if (int.TryParse(idDelStr, out int idDel))
                    {
                        int rows = alunoRepo.Excluir(idDel);
                        Console.WriteLine(rows > 0 ? "✅ Deletado." : "⚠️ Nenhum registro afetado.");
                        await logger.LogAsync(rows > 0
                            ? $"Deletado aluno com Id={idDel}."
                            : $"Nenhum registro deletado para Id={idDel}.");
                    }
                    else
                    {
                        Console.WriteLine("Id inválido.");
                        await logger.LogWarningAsync("Falha ao deletar aluno devido a Id inválido.");
                    }
                    break;

                case "5":
                    Console.Write("Propriedade (coluna): "); var propriedade = Console.ReadLine() ?? "";
                    Console.Write("Valor: "); var valor = Console.ReadLine() ?? "";
                    var resultados = alunoRepo.Buscar(propriedade, valor);
                    Console.WriteLine("== Resultados da Busca ==");
                    foreach (var r in resultados)
                        Console.WriteLine($"#{r.Id} {r.Nome} ({r.Idade}) - {r.Email} - {r.DataNascimento:yyyy-MM-dd}");
                    Console.WriteLine(resultados.Count == 0 ? "(vazio)" : "");
                    await logger.LogAsync($"Buscou pela propriedade '{propriedade}' com valor '{valor}'.");
                    break;

                case "6":
                    var userRepo = new UserRepository(connString);
                    var users = userRepo.Listar();
                    Console.WriteLine("== Lista de Usuários ==");
                    foreach (var u in users)
                        Console.WriteLine($"#{u.Id} {u.Username} ({u.Email}) - {u.FullName} - Role: {u.Role}");
                    Console.WriteLine(users.Count == 0 ? "(vazio)" : "");
                    await logger.LogAsync("Listou todos os usuários.");
                    break;

                default:
                    Console.WriteLine("Opção inválida.");
                    await logger.LogWarningAsync("Opção de menu inválida selecionada.");
                    break;
            }
        }
        catch (SqlException ex)
        {
            Console.WriteLine($"[ERRO SQL] {ex.Number} - {ex.Message}");
            await logger.LogErrorAsync($"Erro SQL {ex.Number}: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERRO] {ex.Message}");
            await logger.LogErrorAsync($"Exceção não tratada: {ex.Message}");
        }
    }
});

Console.WriteLine("=== ADOLab com JWT Authentication ===");
Console.WriteLine("API disponível em: https://localhost:7000");
Console.WriteLine("Swagger UI disponível em: https://localhost:7000/swagger");
Console.WriteLine("Usuário admin padrão: admin / admin123");
Console.WriteLine("Pressione Ctrl+C para parar o servidor");

// Inicia o servidor web
await app.RunAsync();