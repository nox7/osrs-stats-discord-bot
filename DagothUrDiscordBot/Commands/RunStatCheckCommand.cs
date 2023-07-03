using DagothUrDiscordBot.Models;
using DagothUrDiscordBot.OldschoolHiscores;
using DagothUrDiscordBot.OldSchoolHiscores;
using System.Diagnostics;

namespace DagothUrDiscordBot.Commands
{
    internal class RunStatCheckCommand
    {
        public async Task<List<ChangedPlayer>> GetPlayersWithChangedSkills(ulong guildId)
        {
            ChangeTracker changeTracker = new ChangeTracker();
            return await changeTracker.GetPlayersInGuildWithChangedStats(guildId);
        }
    }
}
