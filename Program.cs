using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ADOLabDbContext = ADOLabDbContext;

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

// Configuração do Entity Framework Core
builder.Services.AddDbContext<ADOLabDbContext>(options =>
    options.UseSqlServer(connString));

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

// Registro dos serviços de negócio
builder.Services.AddScoped<AlunoService>();
builder.Services.AddScoped<ProfessorService>();
builder.Services.AddScoped<MatriculaService>();

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
    
    // Garantir que o banco de dados está criado e as tabelas existem
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ADOLabDbContext>();
        await context.Database.EnsureCreatedAsync();
        
        // Verificar se existe usuário administrador
        if (!context.Users.Any(u => u.Username == "admin"))
        {
            var adminPasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
            var adminUser = new User("admin", "admin@adolab.com", adminPasswordHash, "Administrador", "Admin");
            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
            await logger.LogAsync("Usuário administrador padrão criado (username: admin, password: admin123)");
        }
        
        await logger.LogAsync("Banco de dados inicializado com sucesso.");
    }
}
catch (Exception ex)
{
    await logger.LogErrorAsync($"Erro ao inicializar banco de dados: {ex.Message}");
}

Console.WriteLine("=== ADOLab com EF Core e JWT Authentication ===");
Console.WriteLine("API disponível em: https://localhost:7000");
Console.WriteLine("Swagger UI disponível em: https://localhost:7000/swagger");
Console.WriteLine("Usuário admin padrão: admin / admin123");
Console.WriteLine("Pressione Ctrl+C para parar o servidor");

// Inicia o servidor web
await app.RunAsync();