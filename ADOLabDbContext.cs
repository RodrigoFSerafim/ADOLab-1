using Microsoft.EntityFrameworkCore;

/// <summary>
/// Contexto do banco de dados para a aplicação ADOLab.
/// </summary>
public class ADOLabDbContext : DbContext
{
    /// <summary>
    /// Inicializa uma nova instância da classe <see cref="ADOLabDbContext"/>.
    /// </summary>
    /// <param name="options">As opções de configuração do contexto.</param>
    public ADOLabDbContext(DbContextOptions<ADOLabDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Obtém ou define o conjunto de entidades Aluno.
    /// </summary>
    public DbSet<Aluno> Alunos { get; set; } = null!;

    /// <summary>
    /// Obtém ou define o conjunto de entidades Professor.
    /// </summary>
    public DbSet<Professor> Professores { get; set; } = null!;

    /// <summary>
    /// Obtém ou define o conjunto de entidades Disciplina.
    /// </summary>
    public DbSet<Disciplina> Disciplinas { get; set; } = null!;

    /// <summary>
    /// Obtém ou define o conjunto de entidades Matricula.
    /// </summary>
    public DbSet<Matricula> Matriculas { get; set; } = null!;

    /// <summary>
    /// Obtém ou define o conjunto de entidades User.
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Configura o modelo de dados usando Fluent API.
    /// </summary>
    /// <param name="modelBuilder">O construtor do modelo.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuração da entidade Aluno
        modelBuilder.Entity<Aluno>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Configuração da entidade Professor
        modelBuilder.Entity<Professor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Especialidade).HasMaxLength(100);
        });

        // Configuração da entidade Disciplina
        modelBuilder.Entity<Disciplina>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nome).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Codigo).IsRequired().HasMaxLength(20);
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.Property(e => e.Descricao).HasMaxLength(500);

            // Relacionamento com Professor
            entity.HasOne(d => d.Professor)
                  .WithMany(p => p.Disciplinas)
                  .HasForeignKey(d => d.ProfessorId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuração da entidade Matricula
        modelBuilder.Entity<Matricula>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.NotaFinal).HasPrecision(5, 2);
            entity.Property(e => e.Frequencia).HasPrecision(5, 2);

            // Relacionamento com Aluno
            entity.HasOne(m => m.Aluno)
                  .WithMany(a => a.Matriculas)
                  .HasForeignKey(m => m.AlunoId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Relacionamento com Disciplina
            entity.HasOne(m => m.Disciplina)
                  .WithMany(d => d.Matriculas)
                  .HasForeignKey(m => m.DisciplinaId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Índice único para evitar matrículas duplicadas
            entity.HasIndex(e => new { e.AlunoId, e.DisciplinaId }).IsUnique();
        });

        // Configuração da entidade User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
        });

        // Configuração de dados iniciais (Seed Data)
        SeedData(modelBuilder);
    }

    /// <summary>
    /// Configura dados iniciais para o banco de dados.
    /// </summary>
    /// <param name="modelBuilder">O construtor do modelo.</param>
    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Dados iniciais para Professores
        modelBuilder.Entity<Professor>().HasData(
            new Professor
            {
                Id = 1,
                Nome = "Dr. João Silva",
                Email = "joao.silva@universidade.edu",
                Especialidade = "Ciência da Computação",
                DataContratacao = new DateTime(2020, 1, 15),
                Ativo = true
            },
            new Professor
            {
                Id = 2,
                Nome = "Dra. Maria Santos",
                Email = "maria.santos@universidade.edu",
                Especialidade = "Matemática",
                DataContratacao = new DateTime(2019, 8, 20),
                Ativo = true
            }
        );

        // Dados iniciais para Disciplinas
        modelBuilder.Entity<Disciplina>().HasData(
            new Disciplina
            {
                Id = 1,
                Nome = "Programação Orientada a Objetos",
                Codigo = "POO001",
                CargaHoraria = 60,
                Descricao = "Fundamentos da programação orientada a objetos",
                ProfessorId = 1,
                Ativa = true
            },
            new Disciplina
            {
                Id = 2,
                Nome = "Cálculo I",
                Codigo = "CAL001",
                CargaHoraria = 80,
                Descricao = "Fundamentos do cálculo diferencial e integral",
                ProfessorId = 2,
                Ativa = true
            }
        );
    }
}
