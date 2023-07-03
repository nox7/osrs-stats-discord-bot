using DagothUrDiscordBot.Models;
using DagothUrDiscordBot.OldschoolHiscores;
using System.Diagnostics;

namespace DagothUrDiscordBot.Commands
{
    internal class RemoveRSNCommand
    {
        public bool RemoveRSNFromGuildTracking(string rsn, ulong guildId)
        {
            using (var dbContext = new OSRSStatBotDbContext())
            {
                // First, find if the player is tracked
                Player? playerInDb = dbContext.Players.Where(player => player.PlayerName == rsn).FirstOrDefault();
                if (playerInDb != null)
                {
                    var linkQuery = dbContext.PlayerGuildLinks.Where(link => link.PlayerId == playerInDb.Id && link.GuildId == guildId);
                    var link = linkQuery.FirstOrDefault();

                    if (link != null)
                    {
                        var numLinksForThisPlayer = linkQuery.Count();
                        dbContext.PlayerGuildLinks.Remove(link);

                        // Remove the player and their skills. Not tracked by any guild anymore
                        var playerSkills = dbContext.PlayerSkills.Where(skill => skill.PlayerId == playerInDb.Id);

                        foreach (var skill in playerSkills)
                        {
                            dbContext.PlayerSkills.Remove(skill);
                        }

                        dbContext.Players.Remove(playerInDb);
                        dbContext.SaveChanges();

                        return true;
                    }
                }
                else
                {
                    Debug.WriteLine($"{rsn} is not in the local database at all. Cannot remove.");
                }
            }

            return false;
        }
    }
}
