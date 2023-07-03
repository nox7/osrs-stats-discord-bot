using DagothUrDiscordBot.Models;
using DagothUrDiscordBot.OldschoolHiscores;
using Discord;
using System.Numerics;
using System.Text;

namespace DagothUrDiscordBot.Commands
{
    internal class GetTrackedRSNsCommand
    {
        public List<string> GetTrackedRSNsForGuild(ulong guildId)
        {
            List<Player> playersTrackedInGuild = PlayerGuildLink.GetPlayersInGuild(guildId);
            return playersTrackedInGuild.Select(player => player.PlayerName).ToList();
        }

        public Embed GetEmbedOfTrackedPlayerrNames(List<string> playerNames)
        {
            var embed = new EmbedBuilder()
            {
                Title = $"Tracked Players in this Server",
                Description = $"**Number of players**: {playerNames.Count}",
                Color = new Color(255, 0, 0)
            };

            StringBuilder namesAsOneString = new();

            foreach (string name in playerNames)
            {
                namesAsOneString.AppendLine($"- {name}");
            }

            embed.AddField("RSNs", namesAsOneString.ToString());
            return embed.Build();
        }
    }
}
