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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Data Source=DESKTOP-8US9H15;Initial Catalog=dagoth_ur;Trusted_Connection=True;TrustServerCertificate=True");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Players__3214EC0782630384");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.LastDataFetchTimestamp).HasColumnName("last_data_fetch_timestamp");
            entity.Property(e => e.MemberId).HasColumnName("member_id");
            entity.Property(e => e.PlayerName)
                .HasMaxLength(128)
                .HasColumnName("player_name");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
