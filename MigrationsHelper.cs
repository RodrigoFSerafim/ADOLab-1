using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Classe auxiliar para aplicar migrations do Entity Framework Core.
/// </summary>
public static class MigrationsHelper
{
    /// <summary>
    /// Aplica as migrations do banco de dados.
    /// </summary>
    /// <param name="connectionString">A string de conexão com o banco de dados.</param>
    public static async Task ApplyMigrationsAsync(string connectionString)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ADOLabDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        using var context = new ADOLabDbContext(optionsBuilder.Options);

        try
        {
            Console.WriteLine("=== Aplicando Migrations do Entity Framework Core ===");
            
            // Aplicar migrations pendentes
            await context.Database.MigrateAsync();
            
            Console.WriteLine("✅ Migrations aplicadas com sucesso!");
            Console.WriteLine("✅ Banco de dados atualizado com as novas tabelas:");
            Console.WriteLine("   - Professores");
            Console.WriteLine("   - Disciplinas");
            Console.WriteLine("   - Matriculas");
            Console.WriteLine("   - Alunos (atualizada)");
            Console.WriteLine("   - Users (atualizada)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Erro ao aplicar migrations: {ex.Message}");
            throw;
        }
    }
}
