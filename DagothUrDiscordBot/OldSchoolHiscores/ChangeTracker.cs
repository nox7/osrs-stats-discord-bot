using DagothUrDiscordBot.Models;
using DagothUrDiscordBot.OldschoolHiscores;
using System.Diagnostics;
using System.Text;

namespace DagothUrDiscordBot.OldSchoolHiscores
{
    public class ChangeTracker
    {
        public async Task<List<ChangedPlayer>> GetPlayersInGuildWithChangedStats(ulong guildId)
        {
            List<ChangedPlayer> playersWithChangedStats = new();
            List<Player> playersInGuild = PlayerGuildLink.GetPlayersInGuild(guildId);
            var statFetcher = new StatFetcher();

            using (var dbContext = new OSRSStatBotDbContext())
            {
                foreach (Player player in playersInGuild)
                {
                    dbContext.Players.Attach(player);
                    player.LastDataFetchTimestamp = (int) DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    // Fetch their stats
                    HiscoresPlayer? hsPlayer = await statFetcher.GetHiscoresPlayerFromRSN(player.PlayerName);
                    if (hsPlayer != null)
                    {
                        List<ChangedSkill> changedSkills = player.GetAnyChangedSkillsComparedToList(hsPlayer.GetSkillList());
                        if (changedSkills.Count > 0)
                        {
                            Debug.WriteLine($"{player.PlayerName} has skills that have changed since the last fetch.");
                            ChangedPlayer changedPlayer = new ChangedPlayer();
                            changedPlayer.player = player;

                            // Update their total level
                            
                            player.TotalLevel = hsPlayer.GetTotalLevel();
                            player.TotalXP = hsPlayer.GetTotalXP();

                            changedPlayer.changedSkills = changedSkills;
                            playersWithChangedStats.Add(changedPlayer);
                        }
                        else
                        {
                            Debug.WriteLine($"{player.PlayerName} has no changed skills since the last stat fetch.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error fetching player stats for rsn {player.PlayerName}. Did the user change their RSN?");
                    }
                }

                dbContext.SaveChanges();
            }

            return playersWithChangedStats;
        }

        /// <summary>
        /// Used to give Discord a textual format of changed players
        /// </summary>
        /// <param name="changedPlayers"></param>
        /// <returns></returns>
        public string FormatListOfChangedPlayersAsText(List<ChangedPlayer> changedPlayers)
        {
            StringBuilder stringBuilder = new();
            foreach(var changedPlayer in changedPlayers)
            {
                stringBuilder.AppendLine($"=== **{changedPlayer.player.PlayerName}** made gains ===");
                stringBuilder.AppendLine($"== New total level: {changedPlayer.player.TotalLevel} ==");
                foreach(ChangedSkill changedSkill in changedPlayer.changedSkills)
                {
                    int delta = changedSkill.NewLevel - changedSkill.OldLevel;
                    stringBuilder.AppendLine($"{changedSkill.Name} increased by {delta} to {changedSkill.NewLevel}");
                }
            }

            return stringBuilder.ToString();
        }
    }
}
