using DagothUrDiscordBot.Models;
using DagothUrDiscordBot.OldschoolHiscores;
using System.Diagnostics;

namespace DagothUrDiscordBot.Commands
{
    internal class StatWatchCommand
    {
        /// <summary>
        /// Determines if an RSN is already monitored in the database
        /// </summary>
        /// <param name="rsn"></param>
        /// <returns></returns>
        public bool IsRSNMonitoredInGuild(string rsn, ulong guildId)
        {
            using (var dbContext = new OSRSStatBotDbContext())
            {
                // First, find if the player is tracked
                Player? playerInDb = dbContext.Players.Where(player => player.PlayerName == rsn).FirstOrDefault();
                if (playerInDb != null)
                {
                    Debug.WriteLine($"Checking if there is a link to player {rsn} in guild {guildId}");
                    // Return true if there is a link from that player to the provided guild Id
                    return dbContext.PlayerGuildLinks.Any(link => link.PlayerId == playerInDb.Id && link.GuildId == guildId);
                }
                else
                {
                    Debug.WriteLine($"{rsn} is not in the local database at all. Cannot check for a link.");
                }
            }

            Debug.WriteLine($"No link for {rsn} found for guild {guildId}");
            return false;
        }

        /// <summary>
        /// Adds a new RSN to be monitored as a Player that belongs to the provided GuildId
        /// </summary>
        /// <param name="rsn"></param>
        /// <param name="guildId"></param>
        /// <returns></returns>
        public async Task<Player?> AddRSNToBeMonitored(string rsn, ulong guildId)
        {
            using (var dbContext = new OSRSStatBotDbContext())
            {
                var newPlayer = new Player
                {
                    PlayerName = rsn,
                };

                Console.WriteLine($"Looking up new player RSN {rsn} in Old School Runescape Hiscores");
                var statFetcher = new StatFetcher();
                HiscoresPlayer? hiscoresPlayer = await statFetcher.GetHiscoresPlayerFromRSN(rsn);
                if (hiscoresPlayer != null)
                {
                    // Lookup was successful
                    newPlayer.TotalLevel = hiscoresPlayer.GetTotalLevel();
                    newPlayer.TotalXP = hiscoresPlayer.GetTotalXP();
                    newPlayer.LastDataFetchTimestamp = (int) DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    dbContext.Players.Add(newPlayer);
                    dbContext.SaveChanges();

                    var playerToGuildLink = new PlayerGuildLink();
                    playerToGuildLink.GuildId = guildId;
                    playerToGuildLink.PlayerId = newPlayer.Id;
                    dbContext.PlayerGuildLinks.Add(playerToGuildLink);
                    dbContext.SaveChanges();

                    return newPlayer;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
