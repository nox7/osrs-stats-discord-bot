using DagothUrDiscordBot.Models;

namespace DagothUrDiscordBot.Commands
{
    internal class SetAsBotChannelCommand
    {
        public void RegisterChannelAsDefaultBotChannelForGuild(ulong guildId, ulong channelId)
        {
            using (var dbContext = new OSRSStatBotDbContext())
            {
                // Check if one exists
                var existingDefaultChannel = dbContext.GuildDefaultChatChannels.Where(channel => channel.GuildId == guildId && channel.ChannelId == channelId).FirstOrDefault();
                if (existingDefaultChannel != null)
                {
                    existingDefaultChannel.ChannelId = channelId;
                }
                else
                {
                    var newDefaultChannel = new GuildDefaultChatChannel();
                    newDefaultChannel.ChannelId = channelId;
                    newDefaultChannel.GuildId = guildId;
                    dbContext.GuildDefaultChatChannels.Add(newDefaultChannel);
                }

                dbContext.SaveChanges();
            }
        }
    }
}
