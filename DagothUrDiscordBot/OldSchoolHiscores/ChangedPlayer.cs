using DagothUrDiscordBot.Models;
using Discord;
using System.Runtime.CompilerServices;

namespace DagothUrDiscordBot.OldschoolHiscores
{
    public class ChangedPlayer
    {
        public Player player;
        public List<ChangedSkill> changedSkills;

        public Embed GetDiscordEmbedRepresentingChanges()
        {
            var embed = new EmbedBuilder()
            {
                Title = $"{player.PlayerName} Made Gains",
                Description = $"**Total level**: {player.TotalLevel:n0} | **Total XP**: {player.TotalXP:n0}",
                Color = new Color(123, 255, 0)
            };

            foreach(ChangedSkill skill in changedSkills)
            {
                embed.AddField(skill.Name, $"+{skill.NewLevel - skill.OldLevel} | **New level**: {skill.NewLevel}");
            }

            return embed.Build();
        }
    }
}
