using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace DagothUrDiscordBot.Models;

public partial class DagothUrContext : DbContext
{
    public DagothUrContext()
    {
    }

    public DagothUrContext(DbContextOptions<DagothUrContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Player> Players { get; set; }

    public virtual DbSet<PlayerSkill> PlayerSkills { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer(Environment.GetEnvironmentVariable("dbConnection"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerSkill>(entity =>
        {
            entity.HasOne(d => d.Player).WithMany(p => p.PlayerSkills).HasConstraintName("FK_PlayerSkills_Players");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
