using Microsoft.EntityFrameworkCore;
using ShogaiBoard.Models;

namespace ShogaiBoard.Data;

/// <summary>
/// アプリ全体で使用するEF CoreのDbContext。SQLiteファイルに対して読み書きする。
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<TargetSystem> Systems => Set<TargetSystem>();
    public DbSet<Incident> Incidents => Set<Incident>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Severity・Statusは数値ではなく文字列でDBに保存する（DBを直接見たときに値の意味が分かるようにするため）。
        modelBuilder.Entity<Incident>()
            .Property(i => i.Severity)
            .HasConversion<string>();

        modelBuilder.Entity<Incident>()
            .Property(i => i.Status)
            .HasConversion<string>();
    }
}
