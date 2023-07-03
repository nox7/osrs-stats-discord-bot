using System.ComponentModel.DataAnnotations;

namespace DagothUrDiscordBot.Models
{
    public class PlayerGuildLink
    {
        [Key]
        public int Id { get; set; }
        public int PlayerId { get; set; }
        public ulong GuildId { get; set; }

        public static List<Player> GetPlayersInGuild(ulong guildId)
        {
            using (var dbContext = new OSRSStatBotDbContext())
            {
                List<PlayerGuildLink> playerGuildLinks = dbContext.PlayerGuildLinks.Where(link => link.GuildId == guildId).ToList();
                List<int> playerIds = playerGuildLinks.Select(link => link.PlayerId).ToList();
                return dbContext.Players.Where(player => playerIds.Contains(player.Id)).ToList();
            }
        }

        public static List<ulong> GetAllGuildIdsPlayerIsMonitoredBy(int playerId)
        {
            using (var dbContext = new OSRSStatBotDbContext())
            {
                return dbContext.PlayerGuildLinks.Where(link => link.PlayerId == playerId).Select(link => link.GuildId).ToList();
            }
        }
    }
}
