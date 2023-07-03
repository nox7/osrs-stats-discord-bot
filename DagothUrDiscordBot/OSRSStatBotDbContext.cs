using DagothUrDiscordBot.Models;
using Microsoft.EntityFrameworkCore;

namespace DagothUrDiscordBot;

public partial class OSRSStatBotDbContext : DbContext
{
    public virtual DbSet<Player> Players { get; set; }
    public virtual DbSet<PlayerSkill> PlayerSkills { get; set; }
    public virtual DbSet<PlayerGuildLink> PlayerGuildLinks { get; set; }
    public virtual DbSet<GuildDefaultChatChannel> GuildDefaultChatChannels{ get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string? mysqlConnectionString = Environment.GetEnvironmentVariable("dbConnection");
        if (mysqlConnectionString == null)
        {
            Console.WriteLine("Missing dbConnection environment variable.");
            return;
        }

        optionsBuilder.UseMySQL(mysqlConnectionString);
    }
}
